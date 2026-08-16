FROM nginx:alpine

ENV PORTAL_API_URL=http://localhost:8080 \
    PORTAL_AGENT_URL=http://localhost:8090 \
    NGINX_ENVSUBST_OUTPUT_DIR=/usr/share/nginx/html

COPY src/Documentation.Frontend/index.html \
     src/Documentation.Frontend/styles.css \
     src/Documentation.Frontend/helpers.js \
     src/Documentation.Frontend/app.js \
     /usr/share/nginx/html/
COPY src/Documentation.Frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/Documentation.Frontend/config.js.template /etc/nginx/templates/config.js.template

EXPOSE 80
