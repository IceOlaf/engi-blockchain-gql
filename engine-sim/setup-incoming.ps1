# https://docs.aws.amazon.com/powershell/latest/userguide/pstools-sqs-queue-sns-topic.html#pstools-create-sqs-queue

$queueName = "poc-engine-out"
$topicName = "poc-engine-out"

$queueUrl = New-SQSQueue -QueueName $queueName
$queueArn = (Get-SQSQueueAttribute -QueueUrl $queueUrl -AttributeName QueueArn).QueueArn

$topicArn = New-SNSTopic -Name $topicName

$policy = @"
{
    "Version": "2012-10-17",
    "Statement": {
        "Effect": "Allow",
        "Principal": "*",
        "Action": "SQS:SendMessage",
        "Resource": "$queueArn",
        "Condition": { "ArnEquals": { "aws:SourceArn": "$topicArn" } }
    }
}
"@

Set-SQSQueueAttribute -QueueUrl $queueUrl -Attribute @{ Policy=$policy }

Connect-SNSNotification -TopicARN $topicArn -Protocol SQS -Endpoint $queueArn

$accountId = $queueArn.split(':')[4] 

Add-SNSPermission -TopicArn $topicArn -Label ps-cmdlet-topic -AWSAccountIds $accountId -ActionNames publish

Add-SQSPermission -QueueUrl $queueUrl -AWSAccountId $accountId -Label queue-permission -ActionName SendMessage, ReceiveMessage

Publish-SNSMessage -TopicArn $topicArn -Message "Have A Nice Day!"

Receive-SQSMessage -QueueUrl $queueUrl