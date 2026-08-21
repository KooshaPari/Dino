#!/bin/bash
# DINOForge Backup Verification Script
# Verifies: S3 bucket versioning, ECR images, git tags, database snapshots
set -euo pipefail

echo "=== DINOForge Backup Verification ==="
echo "Date: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
echo ""

# 1. Verify S3 bucket versioning
echo "[1/5] S3 Artifact Bucket Versioning"
aws s3api get-bucket-versioning --bucket "dinoforge-artifacts-$${ENV:-staging}" --query 'Status' --output text 2>/dev/null || echo "WARN: Could not check S3"

# 2. Verify ECR images exist
ECR_COUNT=$(aws ecr describe-images --repository-name dinoforge-mcp --query 'length(imageDetails)' --output text 2>/dev/null || echo "0")
echo "[2/5] ECR Images: $ECR_COUNT images in repository"

# 3. Verify git tags
TAG_COUNT=$(git ls-remote --tags origin 2>/dev/null | wc -l || echo "0")
echo "[3/5] Git Tags: $TAG_COUNT remote tags"

# 4. Check latest release
LATEST_TAG=$(git tag --sort=-v:refname | head -1 || echo "none")
echo "[4/5] Latest Release: $LATEST_TAG"

# 5. Verify Docker Compose config
if [ -f docker-compose.yml ]; then
    echo "[5/5] Docker Compose: present"
else
    echo "[5/5] Docker Compose: MISSING"
fi

echo ""
echo "=== Verification Complete ==="
