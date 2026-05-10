output "ecr_repository_url" {
  value = aws_ecr_repository.funds_api.repository_url
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.main.name
}

output "ecs_task_definition_arn" {
  value = aws_ecs_task_definition.funds_api.arn
}

output "cloudwatch_log_group_name" {
  value = aws_cloudwatch_log_group.funds_api.name
}