#!/bin/bash

set -e

if ! command -v awslocal &> /dev/null; then
    echo "awslocal could not be found"
    exit
fi

down() {
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

down "graphql-engine-in.fifo"
down "graphql-engine-out.fifo"
