variable "aws_region" {
  description = "AWS region used to deploy resources"
  type        = string
  default     = "us-east-1"
}

variable "ecr_repository_name" {
  description = "ECR repository name"
  type        = string
  default     = "funds-api"
}