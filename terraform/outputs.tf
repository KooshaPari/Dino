output "ecr_repository_url" {
  description = "ECR repository URL for DINOForge MCP"
  value       = aws_ecr_repository.dinoforge.repository_url
}

output "eks_cluster_endpoint" {
  description = "EKS cluster API endpoint"
  value       = aws_eks_cluster.dinoforge.endpoint
}

output "eks_cluster_name" {
  description = "EKS cluster name"
  value       = aws_eks_cluster.dinoforge.name
}

output "cloudwatch_log_group" {
  description = "CloudWatch log group name"
  value       = aws_cloudwatch_log_group.mcp.name
}

output "s3_artifacts_bucket" {
  description = "S3 bucket for build artifacts"
  value       = aws_s3_bucket.artifacts.id
}
