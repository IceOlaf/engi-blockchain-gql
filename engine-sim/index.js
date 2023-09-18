const AWS = require("aws-sdk");
const { v4: uuid } = require("uuid");

const endpoint = "http://localhost:4566";
const region = "us-east-1";

const sqs = new AWS.SQS({ endpoint, region });
const sns = new AWS.SNS({ endpoint, region });

const incomingQueueUrl =
  "http://localhost:4566/000000000000/graphql-engine-in-test.fifo";

const outgoingTopicArn = `arn:aws:sns:${region}:000000000000:graphql-engine-out-test.fifo`;

const exampleAnalysisResult = {
  repo: "https://github.com/engi-network/demo-csharp.git",
  branch: "master",
  commit: "2bca053",
  technologies: ["CSharp"],
  files: [
    "./PrimeService.Tests/PrimeService_IsPrimeShould.cs",
    "./PrimeService/PrimeService.cs",
  ],
  complexity: {
    sloc: 17,
    cyclomatic: 1.3333333333333333,
  },
  tests: [
    {
      id: "Prime.UnitTests.Services.PrimeService_IsPrimeShould.IsPrime_ValuesLessThan2_ReturnFalse(value: -1)",
      result: "Failed",
      failedResultMessage:
        "System.NotImplementedException : Not fully implemented.",
    },
    {
      id: "Prime.UnitTests.Services.PrimeService_IsPrimeShould.IsPrime_ValuesLessThan2_ReturnFalse(value: 0)",
      result: "Failed",
      failedResultMessage:
        "System.NotImplementedException : Not fully implemented.",
    },
  ],
};

const testAddresses = [
  "5EUJ3p7ds1436scqdA2n6ph9xVs6chshRP1ADjgK1Qj3Hqs2", // test-1
  "5EyiGwuYPgGiEfwpPwXyH5TwXXEUFz6ZgPhzYik2fMCcbqMC", // test-2
  "5G1GQ5bb1bjBUwjSBcArBkbK5gfrW9nTJLhnz3G3nLDo1g5n", // georgiosd
];

async function main() {
  while (true) {
    const data = await sqs
      .receiveMessage({
        QueueUrl: incomingQueueUrl,
        MaxNumberOfMessages: 1,
        WaitTimeSeconds: 20,
      })
      .promise();

    if (!data.Messages) {
      continue;
    }

    for (let i = 0; i < data.Messages.length; ++i) {
      const request = JSON.parse(JSON.parse(data.Messages[i].Body).Message);

      console.log("received request", request);

      const { command } = request;

      let result = null;

      if (command.indexOf("analyse") === 0) {
        result = exampleAnalysisResult;
      } else if (command.indexOf("job attempt") === 0) {
        const [, , url] = command.split(" ");
        const [, , , address] = url.split(/[\/\.]/);

        result = {
          attempt: {
            tests: [
              {
                id: "test-1",
                result: {
                  Failed: "Failed test.",
                }
              },
              {
                id: "test-2",
                result: {
                  Failed: "Failed test.",
                }
              },
            ],
          },
        };

        const callerIndex = testAddresses.indexOf(address);

        if (callerIndex === -1) {
          console.log("Invalid message.");
        } else if (callerIndex === 2) {
          result.attempt.tests.forEach((x) => (x.result = "Passed"));
        } else {
          result.attempt.tests[callerIndex].result = "Passed";
        }
      }

      const response = {
        TopicArn: outgoingTopicArn,
        Message: JSON.stringify({
          identifier: request.identifier,
          stdout: JSON.stringify(result),
          stderr: "",
          returnCode: 0,
        }),
        MessageGroupId: request.identifier,
        MessageDeduplicationId: uuid(),
      };

      await sns.publish(response).promise();

      console.log("published response", response);

      await sqs
        .deleteMessage({
          QueueUrl: incomingQueueUrl,
          ReceiptHandle: data.Messages[0].ReceiptHandle,
        })
        .promise();

      console.log("deleted message");
    }
  }
}

main();
