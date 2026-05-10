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

output "alb_dns_name" {
  value = aws_lb.main.dns_name
}

output "ecs_service_name" {
  value = aws_ecs_service.funds_api.name
}

output "rds_endpoint" {
  value = aws_db_instance.sqlserver.endpoint
}