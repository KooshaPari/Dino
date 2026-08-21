variable "aws_region" {
  description = "AWS region for deployment"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Deployment environment"
  type        = string
  default     = "staging"
  validation {
    condition     = contains(["development", "staging", "production"], var.environment)
    error_message = "Environment must be development, staging, or production."
  }
}

variable "subnet_ids" {
  description = "List of subnet IDs for EKS"
  type        = list(string)
}

variable "log_retention_days" {
  description = "CloudWatch log retention in days"
  type        = number
  default     = 30
}

variable "mcp_cpu" {
  description = "MCP server CPU limit (millicores)"
  type        = number
  default     = 500
}

variable "mcp_memory" {
  description = "MCP server memory limit (MiB)"
  type        = number
  default     = 512
}

variable "mcp_replicas" {
  description = "Number of MCP server replicas"
  type        = number
  default     = 2
}
