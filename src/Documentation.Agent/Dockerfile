FROM python:3.13-slim

WORKDIR /app

COPY src/Documentation.Agent/requirements.txt .
RUN pip install --no-cache-dir --upgrade -r requirements.txt

COPY src/Documentation.Agent/ ./

ENV PYTHONUNBUFFERED=1

EXPOSE 8080

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080", "--workers", "1", "--no-access-log"]
