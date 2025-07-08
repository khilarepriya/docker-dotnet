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
              sh "sonar-scanner -Dsonar.projectKey=${SONAR_PROJECT} -Dsonar.sources=. -Dsonar.host.url=${SONAR_URL}"
            } else if (env.PROJECT_LANG == 'dotnet') {
              withCredentials([string(credentialsId: 'sonarqube-token-new', variable: 'SONAR_TOKEN')]) {
                def projectDir = sh(script: "dirname ${env.CSPROJ_PATH}", returnStdout: true).trim()
                sh """
                  export PATH=\$PATH:\$HOME/.dotnet/tools

                  # Install scanner if not already installed
                  dotnet tool install --global dotnet-sonarscanner || true

                  cd ${projectDir}

                  dotnet sonarscanner begin /k:"${SONAR_PROJECT}" /d:sonar.host.url=${SONAR_URL} /d:sonar.login=\$SONAR_TOKEN
                  dotnet clean
                  dotnet restore
                  dotnet build || { echo "[ERROR] Build failed!"; exit 1; }

                  dotnet sonarscanner end /d:sonar.login=\$SONAR_TOKEN
                """
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


    stage('Stop Service Before Deployment') {
      steps {
        echo "Stopping service ${SERVICE_NAME} before deployment..."
        sh "sudo systemctl stop ${SERVICE_NAME}.service || true"
      }
    }

    stage('Docker Build & Push to Docker Hub') {
      steps {
        script {
          if (!env.DOCKER_HUB_REPO || !env.BUILD_ID || !env.PROJECT_LANG) {
            error("DOCKER_HUB_REPO, BUILD_ID, or PROJECT_LANG is not set.")
          }

          def buildId = env.BUILD_ID ?: env.BUILD_NUMBER
          def lang = env.PROJECT_LANG
          def imageTag = "priyanka015/${lang}:${buildId}"
          def localTag = "${lang}-pipeline-app:latest"

          echo "Using Docker Image Tag: ${imageTag}"

          withCredentials([usernamePassword(credentialsId: 'docker-hub-credentials', usernameVariable: 'DOCKER_USER', passwordVariable: 'DOCKER_PASS')]) {
            def loginCmd = 'echo "$DOCKER_PASS" | docker login -u "$DOCKER_USER" --password-stdin'

            if (lang == 'nodejs') {
             sh """#!/bin/bash
                set -e
                npm install
                ${loginCmd}
                docker build -t "${imageTag}" .
                docker push "${imageTag}"
                docker tag "${imageTag}" "${localTag}"

                echo "Validating Docker image tag..."
                if [[ -z "${imageTag}" ]]; then
                  echo "Image tag is empty. Exiting."
                  exit 1
                fi
                docker pull "${imageTag}" || {
                  echo "Image tag not found. Exiting."
                  exit 1
                }

                echo "[INFO] Preparing systemd unit file..."
                sed "s/__BUILD_ID__/${buildId}/g" /etc/systemd/system/nodejs-app-template.service | sudo tee /etc/systemd/system/nodejs-app.service
                sudo systemctl daemon-reload
                sudo systemctl restart nodejs-app.service
              """

            } else if (lang == 'java') {
              sh """#!/bin/bash
                set -e
                mvn clean package -DskipTests
                ${loginCmd}
                docker build -t "${imageTag}" .
                docker push "${imageTag}"
                sed "s/__BUILD_ID__/${buildId}/g" /etc/systemd/system/java-app-template.service | sudo tee /etc/systemd/system/java-app.service
                sudo systemctl daemon-reload
              """

            } else if (lang == 'python') {
              sh """#!/bin/bash
                set -e
                ${loginCmd}
                docker build -t "${imageTag}" .
                docker push "${imageTag}"
                sed "s/__BUILD_ID__/${buildId}/g" /etc/systemd/system/python-app-template.service | sudo tee /etc/systemd/system/python-app.service
                sudo systemctl daemon-reload
              """

            } else if (lang == 'dotnet') {
              sh """#!/bin/bash
                set -e
                ${loginCmd}
                docker build -t "${imageTag}" .
                docker push "${imageTag}"
                

                # Dynamically create systemd service with current build ID
                echo "[INFO] Generating dotnet-app.service from template using BUILD_ID=${buildId}"
                sed "s/__BUILD_ID__/${buildId}/g" /etc/systemd/system/dotnet-app-template.service | sudo tee /etc/systemd/system/dotnet-app.service
                
                sudo systemctl daemon-reload
                sudo systemctl restart dotnet-app.service
              """

            } else {
              error("Unsupported language: ${lang}")
            }
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
          def deployManifest = """
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
          writeFile file: 'deploy.yaml', text: deployManifest
          
          // Start Minikube if not running and apply manifest
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
            kubectl cluster-info
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
