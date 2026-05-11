resource "aws_ecr_repository" "funds_api" {
  name                 = var.ecr_repository_name
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Project = "case-itau"
  }
}

resource "aws_cloudwatch_log_group" "funds_api" {
  name              = "/ecs/funds-api"
  retention_in_days = 7

  tags = {
    Project = "case-itau"
  }
}

resource "aws_ecs_cluster" "main" {
  name = "funds-api-cluster"

  tags = {
    Project = "case-itau"
  }
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name = "funds-api-ecs-task-execution-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Service = "ecs-tasks.amazonaws.com"
        }
        Action = "sts:AssumeRole"
      }
    ]
  })

  tags = {
    Project = "case-itau"
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role_policy" "ecs_task_execution_secrets_policy" {
  name = "funds-api-ecs-task-execution-secrets-policy"
  role = aws_iam_role.ecs_task_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["secretsmanager:GetSecretValue"]
        Resource = aws_secretsmanager_secret.db_credentials.arn
      }
    ]
  })
}

resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = {
    Name    = "funds-api-vpc"
    Project = "case-itau"
  }
}

resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name    = "funds-api-igw"
    Project = "case-itau"
  }
}

resource "aws_subnet" "public_a" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.1.0/24"
  availability_zone       = "us-east-1a"
  map_public_ip_on_launch = true

  tags = {
    Name    = "funds-api-public-a"
    Project = "case-itau"
  }
}

resource "aws_subnet" "public_b" {
  vpc_id                  = aws_vpc.main.id
  cidr_block              = "10.0.2.0/24"
  availability_zone       = "us-east-1b"
  map_public_ip_on_launch = true

  tags = {
    Name    = "funds-api-public-b"
    Project = "case-itau"
  }
}

resource "aws_subnet" "private_a" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.10.0/24"
  availability_zone = "us-east-1a"

  tags = {
    Name    = "funds-api-private-a"
    Project = "case-itau"
  }
}

resource "aws_subnet" "private_b" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.11.0/24"
  availability_zone = "us-east-1b"

  tags = {
    Name    = "funds-api-private-b"
    Project = "case-itau"
  }
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }

  tags = {
    Name    = "funds-api-public-rt"
    Project = "case-itau"
  }
}

resource "aws_route_table_association" "public_a" {
  subnet_id      = aws_subnet.public_a.id
  route_table_id = aws_route_table.public.id
}

resource "aws_route_table_association" "public_b" {
  subnet_id      = aws_subnet.public_b.id
  route_table_id = aws_route_table.public.id
}

resource "aws_security_group" "alb" {
  name        = "funds-api-alb-sg"
  description = "ALB security group"
  vpc_id      = aws_vpc.main.id

  ingress {
    description = "HTTP from internet"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name    = "funds-api-alb-sg"
    Project = "case-itau"
  }
}

resource "aws_security_group" "ecs" {
  name        = "funds-api-ecs-sg"
  description = "ECS security group"
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "HTTP from ALB"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name    = "funds-api-ecs-sg"
    Project = "case-itau"
  }
}

resource "aws_security_group" "rds" {
  name        = "funds-api-rds-sg"
  description = "RDS security group"
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "SQL Server from ECS"
    from_port       = 1433
    to_port         = 1433
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name    = "funds-api-rds-sg"
    Project = "case-itau"
  }
}

resource "aws_security_group" "redis" {
  name        = "funds-api-redis-sg"
  description = "ElastiCache Redis security group"
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "Redis from ECS"
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [aws_security_group.ecs.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name    = "funds-api-redis-sg"
    Project = "case-itau"
  }
}

resource "aws_secretsmanager_secret" "db_credentials" {
  name = "funds-api-db-credentials"

  tags = {
    Project = "case-itau"
  }
}

resource "aws_db_subnet_group" "main" {
  name = "funds-api-db-subnet-group"

  subnet_ids = [
    aws_subnet.private_a.id,
    aws_subnet.private_b.id
  ]

  tags = {
    Name    = "funds-api-db-subnet-group"
    Project = "case-itau"
  }
}

resource "aws_db_instance" "sqlserver" {
  identifier = "funds-api-sqlserver"

  engine         = "sqlserver-ex"
  engine_version = "15.00"

  instance_class = "db.t3.micro"

  allocated_storage = 20

  username = "admin"
  password = "CaseItau123!"

  db_subnet_group_name = aws_db_subnet_group.main.name

  vpc_security_group_ids = [
    aws_security_group.rds.id
  ]

  publicly_accessible = false

  skip_final_snapshot     = true
  multi_az                = false
  storage_type            = "gp2"
  backup_retention_period = 0
  deletion_protection     = false

  tags = {
    Name    = "funds-api-sqlserver"
    Project = "case-itau"
  }
}

resource "aws_secretsmanager_secret_version" "db_credentials" {
  secret_id = aws_secretsmanager_secret.db_credentials.id

  secret_string = jsonencode({
    username         = "admin"
    password         = "CaseItau123!"
    connectionString = "Server=${aws_db_instance.sqlserver.address},1433;Database=FundsDb;User Id=admin;Password=CaseItau123!;TrustServerCertificate=True"
  })
}

resource "aws_elasticache_subnet_group" "redis" {
  name = "funds-api-redis-subnet-group"

  subnet_ids = [
    aws_subnet.private_a.id,
    aws_subnet.private_b.id
  ]

  tags = {
    Name    = "funds-api-redis-subnet-group"
    Project = "case-itau"
  }
}

resource "aws_elasticache_replication_group" "redis" {
  replication_group_id = "funds-api-redis"
  description          = "Redis cache for funds-api"

  engine         = "redis"
  engine_version = "7.0"
  node_type      = "cache.t3.micro"

  num_cache_clusters         = 1
  automatic_failover_enabled = false
  multi_az_enabled           = false

  subnet_group_name = aws_elasticache_subnet_group.redis.name

  security_group_ids = [
    aws_security_group.redis.id
  ]

  port = 6379

  at_rest_encryption_enabled = true
  transit_encryption_enabled = false

  tags = {
    Name    = "funds-api-redis"
    Project = "case-itau"
  }
}

resource "aws_ecs_task_definition" "funds_api" {
  family                   = "funds-api-task"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn

  container_definitions = jsonencode([
    {
      name      = "funds-api"
      image     = "${aws_ecr_repository.funds_api.repository_url}:latest"
      essential = true

      portMappings = [
        {
          containerPort = 8080
          hostPort      = 8080
          protocol      = "tcp"
        }
      ]

      environment = [
        {
          name  = "ASPNETCORE_ENVIRONMENT"
          value = "Production"
        },
        {
          name  = "ASPNETCORE_URLS"
          value = "http://+:8080"
        },
        {
          name  = "ConnectionStrings__Redis"
          value = "${aws_elasticache_replication_group.redis.primary_endpoint_address}:6379"
        }
      ]

      secrets = [
        {
          name      = "ConnectionStrings__DefaultConnection"
          valueFrom = "${aws_secretsmanager_secret.db_credentials.arn}:connectionString::"
        }
      ]

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          awslogs-group         = aws_cloudwatch_log_group.funds_api.name
          awslogs-region        = var.aws_region
          awslogs-stream-prefix = "ecs"
        }
      }
    }
  ])

  tags = {
    Project = "case-itau"
  }
}

resource "aws_lb" "main" {
  name               = "funds-api-alb"
  load_balancer_type = "application"
  internal           = false

  security_groups = [
    aws_security_group.alb.id
  ]

  subnets = [
    aws_subnet.public_a.id,
    aws_subnet.public_b.id
  ]

  tags = {
    Name    = "funds-api-alb"
    Project = "case-itau"
  }
}

resource "aws_lb_target_group" "funds_api" {
  name        = "funds-api-tg"
  port        = 8080
  protocol    = "HTTP"
  target_type = "ip"
  vpc_id      = aws_vpc.main.id

  health_check {
    path                = "/health/ready"
    protocol            = "HTTP"
    matcher             = "200-399"
    interval            = 30
    timeout             = 5
    healthy_threshold   = 2
    unhealthy_threshold = 3
  }

  tags = {
    Name    = "funds-api-tg"
    Project = "case-itau"
  }
}

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.funds_api.arn
  }
}

resource "aws_ecs_service" "funds_api" {
  name            = "funds-api-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.funds_api.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets = [
      aws_subnet.public_a.id,
      aws_subnet.public_b.id
    ]

    security_groups = [
      aws_security_group.ecs.id
    ]

    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.funds_api.arn
    container_name   = "funds-api"
    container_port   = 8080
  }

  depends_on = [
    aws_lb_listener.http,
    aws_db_instance.sqlserver,
    aws_secretsmanager_secret_version.db_credentials,
    aws_elasticache_replication_group.redis
  ]

  tags = {
    Name    = "funds-api-service"
    Project = "case-itau"
  }
}