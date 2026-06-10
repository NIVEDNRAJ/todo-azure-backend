pipeline {
    agent any

    environment {
        ACR_NAME = '<acr-name>' // User to replace with actual ACR name
        ACR_REGISTRY = "${ACR_NAME}.azurecr.io"
        IMAGE_NAME = 'todo-backend'
        AKS_CREDENTIALS_ID = 'aks-kubeconfig'
        ACR_CREDENTIALS_ID = 'acr-credentials'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Application') {
            steps {
                bat 'dotnet restore'
                bat 'dotnet build --configuration Release'
            }
        }

        stage('Build Docker Image') {
            steps {
                bat "docker build --no-cache -t ${IMAGE_NAME}:latest -t ${IMAGE_NAME}:${BUILD_NUMBER} ."
            }
        }

        stage('Push to ACR') {
            steps {
                script {
                    withCredentials([usernamePassword(credentialsId: "${ACR_CREDENTIALS_ID}", usernameVariable: 'ACR_USER', passwordVariable: 'ACR_PASS')]) {
                        bat "docker login ${ACR_REGISTRY} -u ${ACR_USER} -p ${ACR_PASS}"
                        bat "docker tag ${IMAGE_NAME}:latest ${ACR_REGISTRY}/${IMAGE_NAME}:latest"
                        bat "docker tag ${IMAGE_NAME}:latest ${ACR_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER}"
                        bat "docker push ${ACR_REGISTRY}/${IMAGE_NAME}:latest"
                        bat "docker push ${ACR_REGISTRY}/${IMAGE_NAME}:${BUILD_NUMBER}"
                    }
                }
            }
        }

        stage('Validate Kubernetes Manifests') {
            steps {
                script {
                    withCredentials([file(credentialsId: "${AKS_CREDENTIALS_ID}", variable: 'KUBECONFIG')]) {
                        bat "kubectl --kubeconfig=%KUBECONFIG% apply --dry-run=client -f sql-deployment-service.yml"
                        bat "kubectl --kubeconfig=%KUBECONFIG% apply --dry-run=client -f backend-deployment-service.yml"
                    }
                }
            }
        }

        stage('Deploy to AKS') {
            steps {
                script {
                    withCredentials([file(credentialsId: "${AKS_CREDENTIALS_ID}", variable: 'KUBECONFIG')]) {
                        bat "kubectl --kubeconfig=%KUBECONFIG% apply -f sql-deployment-service.yml"
                        bat "kubectl --kubeconfig=%KUBECONFIG% apply -f backend-deployment-service.yml"
                    }
                }
            }
        }
    }

    post {
        success {
            echo "Backend pipeline completed successfully!"
        }
        failure {
            echo "Backend pipeline failed. Please check the logs."
        }
    }
}
