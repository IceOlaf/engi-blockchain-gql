const AWS = require("aws-sdk");

const endpoint = "http://localhost:4566"

const sqs = new AWS.SQS({ endpoint });
const sns = new AWS.SNS({ endpoint });

const incomingQueueUrl =
  "http://localhost:4566/000000000000/graphql-engine-in.fifo";

const outgoingTopicArn = "arn:aws:sns:us-east-1:000000000000:graphql-engine-out.fifo";

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

      const response = {
        TopicArn: outgoingTopicArn,
        Message: JSON.stringify({
          identifier: request.identifier,
          stdout: JSON.stringify(exampleAnalysisResult),
          stderr: "",
          returnCode: 0,
        }),
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
