#!/bin/bash

set -e

if ! command -v awslocal &> /dev/null; then
    echo "awslocal could not be found"
    exit
fi

if ! command -v jq &> /dev/null
then
    echo "installing jq"
    apt update && apt install jq gettext -y
fi

up() {
    QUEUE_NAME=$1
    TOPIC_NAME=$1

    if awslocal sqs list-queues | jq -r .QueueUrls[] | grep -q $QUEUE_NAME; then
        echo "Queue $QUEUE_NAME already exists; execute 'localstack-down.sh'."
        exit 1
    fi

    if awslocal sns list-topics | jq -r .Topics[].TopicArn | grep -q $TOPIC_NAME; then
        echo "Topic $TOPIC_NAME already exists; execute 'localstack-down.sh'."
        exit 1
    fi

    QUEUE_URL=`awslocal sqs create-queue --queue-name $QUEUE_NAME --attributes FifoQueue=true | jq -r .QueueUrl`

    export QUEUE_ARN=`awslocal sqs get-queue-attributes --queue-url $QUEUE_URL --attribute-names QueueArn | jq -r .Attributes.QueueArn`
    export TOPIC_ARN=`awslocal sns create-topic --name $TOPIC_NAME --attributes FifoTopic=true | jq -r .TopicArn`

    POLICY=`echo '{ "Version": "2012-10-17", "Statement": { "Effect": "Allow", "Principal": "*", "Action": "SQS:SendMessage", "Resource": "${QUEUE_ARN}", "Condition": { "ArnEquals": { "aws:SourceArn": "${TOPIC_ARN}" } } } }' | envsubst`

    echo $POLICY > ./policy.json
    awslocal sqs set-queue-attributes --queue-url $QUEUE_URL --attributes Policy=file://./policy.json
    rm ./policy.json

    awslocal sns add-permission --topic-arn $TOPIC_ARN --label queue-permission --action-name Publish --aws-account-id 000000000000

    awslocal sqs add-permission --queue-url $QUEUE_URL --label queue-permission --actions SendMessage --actions ReceiveMessage --aws-account-id 000000000000

    awslocal sns subscribe --topic-arn $TOPIC_ARN --protocol sqs --notification-endpoint $QUEUE_ARN

    ROLE_POLICY='{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":"*","Resource":"*"}]}'
    awslocal iam create-role --role-name graphql-testing --assume-role-policy-document "${ROLE_POLICY}"

    echo "Created $1 queue, topic, subscription."
}

up "graphql-engine-in.fifo"
up "graphql-engine-out.fifo"
