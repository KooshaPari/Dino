# DINOForge Infrastructure as Code
# Provisions: Container Registry, Managed K8s, Monitoring, DNS

terraform {
  required_version = ">= 1.5"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

# ECR - Container Registry
resource "aws_ecr_repository" "dinoforge" {
  name                 = "dinoforge-mcp"
  image_tag_mutability = "MUTABLE"
  image_scanning_configuration {
    scan_on_push = true
  }
}

# EKS - Managed Kubernetes
resource "aws_eks_cluster" "dinoforge" {
  name     = "dinoforge-${var.environment}"
  role_arn = aws_iam_role.eks.arn
  vpc_config {
    subnet_ids = var.subnet_ids
  }
}

resource "aws_iam_role" "eks" {
  name = "dinoforge-eks-role-${var.environment}"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = { Service = "eks.amazonaws.com" }
    }]
  })
}

# CloudWatch - Log Groups
resource "aws_cloudwatch_log_group" "mcp" {
  name              = "/dinoforge/mcp-${var.environment}"
  retention_in_days = var.log_retention_days
}

# S3 - Artifact Storage
resource "aws_s3_bucket" "artifacts" {
  bucket = "dinoforge-artifacts-${var.environment}"
}

resource "aws_s3_bucket_versioning" "artifacts" {
  bucket = aws_s3_bucket.artifacts.id
  versioning_configuration {
    status = "Enabled"
  }
}
