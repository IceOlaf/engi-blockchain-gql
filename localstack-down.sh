#!/bin/bash

set -e

if ! command -v awslocal &> /dev/null; then
    echo "awslocal could not be found"
    exit
fi

down_iam() {
    if awslocal iam get-role --role-name graphql-testing &> /dev/null; then
      awslocal iam delete-role --role-name graphql-testing
      echo "Cleaned up IAM role."
    fi
}

down_sns_sqs_pair() {
    QUEUE_NAME=$1
    TOPIC_NAME=$1

    if awslocal sqs list-queues | jq -r .QueueUrls[] | grep -q $QUEUE_NAME; then
        QUEUE_URL=`awslocal sqs get-queue-url --queue-name $QUEUE_NAME | jq -r .QueueUrl`
        awslocal sqs delete-queue --queue-url $QUEUE_URL
        echo "Cleaned up $1 queue."
    fi

    if awslocal sns list-topics | jq -r .Topics[].TopicArn | grep -q $TOPIC_NAME; then
        TOPIC_ARN=`awslocal sns list-topics | jq -r .Topics[].TopicArn | grep $TOPIC_NAME`
        awslocal sns delete-topic --topic-arn $TOPIC_ARN
        echo "Cleaned up $1 topic."
    fi
}

down_iam
down_sns_sqs_pair "graphql-engine-in-test.fifo"
down_sns_sqs_pair "graphql-engine-out-test.fifo"
