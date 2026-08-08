FROM python:3.13-slim

WORKDIR /app

COPY src/Documentation.Agent/requirements.txt .
RUN pip install --no-cache-dir \
        --index-url https://download.pytorch.org/whl/cpu \
        torch==2.13.0 \
    && pip install --no-cache-dir --upgrade -r requirements.txt

COPY src/Documentation.Agent/app.py src/Documentation.Agent/test_app.py ./

ENV HF_HOME=/models/huggingface \
    PYTHONUNBUFFERED=1

EXPOSE 8080

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080", "--workers", "1"]
