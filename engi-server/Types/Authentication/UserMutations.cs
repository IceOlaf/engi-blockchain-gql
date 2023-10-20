using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Engi.Substrate.Identity;
using Engi.Substrate.Keys;
using GraphQL;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Types;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using SessionOptions = Raven.Client.Documents.Session.SessionOptions;

namespace Engi.Substrate.Server.Types.Authentication;

public class UserMutations : ObjectGraphType
{
    public UserMutations()
    {
        Field<IdGraphType>("importKey")
            .Description(@"
                Import a user's key, to be managed by ENGI. The user is located with the key's address.
                If the key doesn't match a user's address on file, code NOT_FOUND is returned.
                If the key cannot be decoded, code INVALID_KEY is returned.  
                This mutation does not require authentication so it can be invoked directly after 
                registration (login would require e-mail confirmation).
            ")
            .Argument<NonNullGraphType<ImportUserKeyArgumentsGraphType>>("args")
            .ResolveAsync(ImportUserKeyAsync)
            .AllowAnonymous();

        Field<CurrentUserInfoGraphType>("update")
            .Description(@"
                Update current user.
            ")
            .Argument<NonNullGraphType<UpdateUserArgumentsGraphType>>("args")
            .ResolveAsync(UpdateUserAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<StringGraphType>("getProfileImagePreSignedUrl")
            .Description(@"
                Generates an S3 pre-signed URL to upload an avatar image with the specified content type.
            ")
            .Argument<string>("contentType")
            .ResolveAsync(GetProfileImagePreSignedUrlAsync)
            .AuthorizeWithPolicy(PolicyNames.Authenticated);
    }

    private async Task<object?> GetProfileImagePreSignedUrlAsync(IResolveFieldContext<object?> context)
    {
        string contentType = context.GetArgument<string>("contentType");

        string ext = contentType switch
        {
            "image/png" => "png",
            "image/jpg" => "jpg",
            "image/jpeg" => "jpg",
            _ => throw new ExecutionError("Invalid content type")
            {
                Code = "INVALID_CONTENT_TYPE"
            }
        };

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var awsOptions = scope.ServiceProvider.GetRequiredService<IOptions<AwsOptions>>().Value;

        var s3 = CreateS3Client(awsOptions);

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = awsOptions.BucketName,
            Key = $"{GetProfileImagePrefix(user)}_{Guid.NewGuid()}.{ext}",
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(5)
        };

        return s3.GetPreSignedURL(request);
    }

    private async Task<object?> ImportUserKeyAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<ImportUserKeyArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        // make sure we can decode the key

        var userCrypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        Keypair keypair;

        try
        {
            keypair = userCrypto.DecryptKeypair(args.EncryptedPkcs8Key);
        }
        catch (Exception)
        {
            throw new ExecutionError("Unable to decrypt and decode key.")
            {
                Code = "INVALID_KEY"
            };
        }

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var userAddressRef = await session
            .LoadAsync<UserAddressReference>(UserAddressReference.KeyFrom(keypair.Address),
                include => include.IncludeDocuments(x => x.UserId));

        if (userAddressRef == null)
        {
            throw new ExecutionError("User is not registered")
            {
                Code = "NOT_FOUND"
            };
        }

        var user = session.LoadAsync<User>(userAddressRef.UserId).Result;
        
        user.KeypairPkcs8 = userCrypto.EncryptKeypair(keypair);

        await session.SaveChangesAsync();

        return null;
    }

    private async Task<object?> UpdateUserAsync(IResolveFieldContext<object?> context)
    {
        var args = context.GetValidatedArgument<UpdateUserArguments>("args");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IDocumentStore>()
            .OpenAsyncSession(new SessionOptions
            {
                TransactionMode = TransactionMode.ClusterWide
            });

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        if (!string.IsNullOrEmpty(args.Email)
            && !string.Equals(args.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            string emailReferenceKey = UserEmailReference.KeyFrom(user.Email);

            session.Delete(emailReferenceKey);

            // update

            user.Email = args.Email.ToLowerInvariant();

            await session.StoreAsync(new UserEmailReference(user));
        }

        if (!string.IsNullOrEmpty(args.Display))
        {
            user.Display = args.Display;
        }

        if (!string.IsNullOrEmpty(args.ProfileImageUrl))
        {
            var awsOptions = scope.ServiceProvider.GetRequiredService<IOptions<AwsOptions>>().Value;

            if (!AmazonS3Uri.TryParseAmazonS3Uri(args.ProfileImageUrl, out var s3Uri)
               || !string.Equals(s3Uri.Bucket, awsOptions.BucketName, StringComparison.OrdinalIgnoreCase)
               || !s3Uri.Key.StartsWith(GetProfileImagePrefix(user), StringComparison.OrdinalIgnoreCase))
            {
                throw new AccessDeniedError(nameof(args.ProfileImageUrl));
            }

            // var s3 = CreateS3Client(awsOptions);

            // var request = new GetObjectAttributesRequest
            // {
            //     BucketName = awsOptions.BucketName,
            //     Key = s3Uri.Key,
            //     ObjectAttributes = new List<ObjectAttributes> { ObjectAttributes.ObjectSize }
            // };

            // var attributes = await s3.GetObjectAttributesAsync(request);

            // if (attributes.ObjectSize == 0)
            // {
            //     throw new ExecutionError("File has size zero.");
            // }

            user.ProfileImageUrl = args.ProfileImageUrl;
        }

        if (args.FreelancerSettings != null)
        {
            user.FreelancerSettings = args.FreelancerSettings;
        }

        if (args.BusinessSettings != null)
        {
            user.BusinessSettings = args.BusinessSettings;
        }

        if (args.EmailSettings != null)
        {
            user.EmailSettings = args.EmailSettings;
        }

        try
        {
            await session.SaveChangesAsync();
        }
        catch (ConcurrencyException)
        {
            throw new ExecutionError("E-mail conflict.")
            {
                Code = "EMAIL_CONFLICT"
            };
        }

        return (CurrentUserInfo) user;
    }

    private static string GetProfileImagePrefix(User user)
    {
        return $"profile_images/{user.Address}";
    }

    private static AmazonS3Client CreateS3Client(AwsOptions awsOptions)
    {
        return new AmazonS3Client(
            new AmazonS3Config().Apply(awsOptions));
    }
}
