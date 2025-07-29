pipeline {
  agent any

  environment {
    SONAR_URL = 'http://ec2-13-202-47-19.ap-south-1.compute.amazonaws.com:15998/'
    SONAR_PROJECT = 'docker-dotnet'
    EMAIL_RECIPIENT = 'p.khilare@accenture.com'
    SERVICE_NAME = 'dotnet-app'
    DOCKER_HUB_REPO = 'priyanka015/dotnet'
    KUBECONFIG = "/var/lib/jenkins/.kube/config"
    MINIKUBE_HOME = '/var/lib/jenkins'
    PYTHONPATH = "${env.WORKSPACE}"
    TESTCONTAINERS_RYUK_DISABLED=true
  }

  stages {
    stage('Checkout Code') {
      steps {
        git branch: 'main', credentialsId: 'github-token', url: 'https://github.com/khilarepriya/docker-dotnet.git'
      }
    }

    stage('Detect Language') {
      steps {
        script {
          if (fileExists('pom.xml')) {
            env.PROJECT_LANG = 'java'
          } else if (fileExists('package.json')) {
            env.PROJECT_LANG = 'nodejs'
          } else if (fileExists('requirements.txt') || fileExists('pyproject.toml')) {
            env.PROJECT_LANG = 'python'
          } else {
            def csprojPath = sh(
              script: '''
                find . -name '*.csproj' | grep -vi test | head -n 1
              ''',
              returnStdout: true
            ).trim()

            if (csprojPath) {
              env.PROJECT_LANG = 'dotnet'
              env.CSPROJ_PATH = csprojPath // Optional: can be used in later stages
            } else {
              error("Unsupported project type: No recognizable project file found.")
            }
          }
          echo "Detected language: ${env.PROJECT_LANG}"
          if (env.PROJECT_LANG == 'dotnet') {
            echo "Detected .csproj path: ${env.CSPROJ_PATH}"
        }
      }
    }
  }  

    stage('SonarQube Scan') {
      steps {
        script {
          withSonarQubeEnv('SonarQube') {
            if (env.PROJECT_LANG == 'java') {
              sh 'mvn clean compile sonar:sonar -Dsonar.java.binaries=target/classes'
            } else if (env.PROJECT_LANG == 'python' || env.PROJECT_LANG == 'nodejs') {
              sh """
                sonar-scanner \
                  -Dsonar.projectKey=${SONAR_PROJECT} \
                  -Dsonar.sources=. \
                  -Dsonar.host.url=${SONAR_URL}
              """
            } else if (env.PROJECT_LANG == 'dotnet') {
              withCredentials([string(credentialsId: 'sonarqube-token-new', variable: 'SONAR_TOKEN')]) {
                def projectDir = sh(script: "dirname ${env.CSPROJ_PATH}", returnStdout: true).trim()
                dir(projectDir) {
                  sh """
                    echo "📦 Starting .NET SonarQube analysis..."

                    export DOTNET_ROOT=/home/p_khilare/.dotnet
                    export PATH=\$DOTNET_ROOT:\$DOTNET_ROOT/tools:\$PATH

                    # Use local tool manifest
                    dotnet new tool-manifest --force
                    dotnet tool install dotnet-sonarscanner || true

                    # Start SonarQube analysis using local tool
                    dotnet tool run dotnet-sonarscanner begin \
                      /k:"${SONAR_PROJECT}" \
                      /d:sonar.host.url=${SONAR_URL} \
                      /d:sonar.login=\$SONAR_TOKEN

                    dotnet clean
                    dotnet restore
                    dotnet build || { echo "[ERROR] Build failed!"; exit 1; }

                    # End SonarQube analysis using local tool
                    dotnet tool run dotnet-sonarscanner end \
                      /d:sonar.login=\$SONAR_TOKEN
                  """ 
                }
              }
            }
          }
        }
      }
    }

    stage('Snyk Scan') {
      steps {
        withCredentials([string(credentialsId: 'snyk-token01', variable: 'SNYK_TOKEN')]) {
          script {
            if (env.PROJECT_LANG == 'python') {
              sh '''
                python3 -m venv venv
                . venv/bin/activate
                pip install -r requirements.txt || true
                snyk auth $SNYK_TOKEN
                snyk test --file=requirements.txt --package-manager=pip --severity-threshold=high || true
                snyk monitor --file=requirements.txt --package-manager=pip
              '''
            } else if (env.PROJECT_LANG == 'java') {
              sh '''
                snyk auth $SNYK_TOKEN
                snyk test --file=pom.xml --package-manager=maven --severity-threshold=high || true
                snyk monitor --file=pom.xml --package-manager=maven
              '''
            } else if (env.PROJECT_LANG == 'nodejs') {
              sh '''
                npm install
                snyk auth $SNYK_TOKEN
                snyk test --file=package.json --package-manager=npm --severity-threshold=high || true
                snyk monitor --file=package.json --package-manager=npm
              '''
            } else if (env.PROJECT_LANG == 'dotnet') {
              def csprojDir = sh(script: "dirname ${env.CSPROJ_PATH}", returnStdout: true).trim()
              sh """
                snyk auth \$SNYK_TOKEN

                # Navigate to the directory containing the .csproj
                cd "${csprojDir}"

                dotnet restore

                # Run Snyk without --file
                snyk test --severity-threshold=high || true
                snyk monitor 
              """
            }
          }
        }
      }
    }
  
    stage('Unit Test with Testcontainers') {
      steps {
        script {
          if (env.PROJECT_LANG == 'dotnet') {
            def appDir = "src/DotNetApp"
            def testDir = "tests/DotNetApp.Tests"

            // ✅ Step 1: Build main app (no Docker build here!)
            dir(appDir) {
              sh '''
                echo 📦 Restoring and building main app...
                dotnet restore
                dotnet build --no-restore
              '''
            }

            // ✅ Step 2: Build Docker image from repo root & run tests
            dir(testDir) {
              sh '''
                echo 🐳 Building docker image 'dotnetapp:latest' for Testcontainers...
                docker build -t dotnetapp:latest -f ../../Dockerfile ../../

                echo 🧪 Restoring, building, and testing...
                dotnet restore
                dotnet build
                TEST_IMAGE_NAME=dotnetapp:latest TESTCONTAINERS_RYUK_DISABLED=true dotnet test --logger:trx
              '''
            }
          } else if (env.PROJECT_LANG == 'java') {
            sh 'mvn clean test'
          } else if (env.PROJECT_LANG == 'python') {
            sh '''#!/bin/bash
              set -e
              rm -rf venv
              python3 -m venv venv
              source venv/bin/activate
              pip install --upgrade pip
              pip install -r requirements.txt
              pip install testcontainers pytest
              export PYTHONPATH=$PWD
              pytest
            '''
          } else if (env.PROJECT_LANG == 'nodejs') {
            sh '''
              npm install
              npm install --save-dev jest testcontainers
              npx jest
            '''
          }
        }
      }
    }
  
    stage('Set Image Name') {
      steps {
        script {
          env.IMAGE_NAME = "${DOCKER_HUB_REPO}:${BUILD_ID}"
          echo "✅ Image name set: ${env.IMAGE_NAME}"
        }
      }
    }

    stage('Generate Kubernetes YAML') {
      steps {
        script {
          def deployManifest = """\
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ${env.PROJECT_LANG}-app
spec:
  replicas: 1
  selector:
    matchLabels:
      app: ${env.PROJECT_LANG}-app
  template:
    metadata:
      labels:
        app: ${env.PROJECT_LANG}-app
    spec:
      containers:
        - name: ${env.PROJECT_LANG}-container
          image: ${env.IMAGE_NAME}
          ports:
            - containerPort: 80
"""
          writeFile file: 'deploy.yaml', text: deployManifest.trim() + '\n'
          echo "✅ Generated clean deploy.yaml"
          sh 'cat deploy.yaml'
        }
      }
    }

    stage('YAML Lint Validation') {
      steps {
        sh '''
          if [ -f deploy.yaml ]; then
            echo "🔍 Linting deploy.yaml"
            yamllint deploy.yaml
          else
            echo "⚠️ deploy.yaml not found!"
            exit 1
          fi
        '''
      }
    }

    stage('Stop Service Before Deployment') {
      steps {
        echo "Stopping service ${SERVICE_NAME} before deployment..."
        sh "sudo systemctl stop ${SERVICE_NAME}.service || true"
      }
    }

    stage('Docker Build & Push to Docker Hub') {
      steps {
        script {
          withCredentials([usernamePassword(
            credentialsId: 'docker-hub-credentials',
            usernameVariable: 'DOCKER_USER',
            passwordVariable: 'DOCKER_PASS'
          )]) {
            sh '''
              echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin
              docker build -t $DOCKER_HUB_REPO:$BUILD_ID .
              docker push $DOCKER_HUB_REPO:$BUILD_ID
              echo "✅ Docker image pushed successfully!"
            '''
          }
        }
      }
    }

    stage('Start Service After Deployment') {
      steps {
        echo "Starting service ${SERVICE_NAME} after deployment..."
        sh "sudo systemctl start ${SERVICE_NAME}.service"
      }
    }

    stage('Deploy to Kubernetes (Minikube)') {
      steps {
        script {
          sh '''
            echo "➡️  Starting or checking Minikube..."
            export MINIKUBE_HOME=/var/lib/jenkins
            export KUBECONFIG=/var/lib/jenkins/.kube/config

            CURRENT_VERSION=$(minikube status --format '{{.Kubeconfig.Version}}' 2>/dev/null || echo "none")
            echo "🌀 Current Minikube K8s version: $CURRENT_VERSION"

            if [[ "$CURRENT_VERSION" != "v1.27.4" ]]; then
              echo "⚠️ Recreating Minikube with required version..."
              minikube delete || true
              minikube start --kubernetes-version=v1.27.4 --cpus=2 --memory=8192 --driver=docker
            else
              minikube start
            fi

            echo "✅ Using KUBECONFIG at $KUBECONFIG"
            kubectl config use-context minikube
            kubectl apply -f deploy.yaml
          '''
        }
      }
    } 

    stage('Sanity Test') {
      steps {
        script {
          sh """#!/bin/bash
            echo "Waiting for service to stabilize..."
            sleep 5
            curl -f http://localhost:6060/health || {
              echo "[ERROR] Sanity test failed!"
              exit 1
            }
            echo "[INFO] Sanity test passed."
          """
        }
      }
    }
  }

  post {
    always {
      mail to: "${EMAIL_RECIPIENT}",
           subject: "CI/CD Pipeline Report for ${env.JOB_NAME}",
           body: """\
Hello,

The pipeline has completed.

🔍 SonarQube Report: ${SONAR_URL}dashboard?id=${SONAR_PROJECT}  
📦 Snyk Project Page: https://app.snyk.io/org/your-org/project?q=${SONAR_PROJECT}

Please review for any issues.

Thanks,  
Jenkins Pipeline
"""
      cleanWs()
    }
  }
}
