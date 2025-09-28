#!/bin/bash

# Backend 실행 스크립트

echo "🚀 Starting Azure Auth Demo Backend..."

# Backend 디렉토리로 이동
cd Backend

# 패키지 복원
echo "📦 Restoring packages..."
dotnet restore

# 데이터베이스 확인 및 생성
if [ ! -f "app.db" ]; then
    echo "🗄️ Creating database..."
    dotnet ef migrations add InitialCreate
    dotnet ef database update
else
    echo "✅ Database already exists"
fi

# 서버 실행
echo "🌐 Starting server on http://localhost:5000"
echo "📖 Swagger UI available at http://localhost:5000/swagger"
dotnet run
