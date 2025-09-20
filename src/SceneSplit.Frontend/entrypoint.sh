#!/bin/sh
cat <<EOF > /usr/share/nginx/html/assets/config.json
{
  "hubUrl": "${HUB_URL}",
  "maxFileSize": "${MAX_FILE_SIZE}",
  "allowedImageTypes": "$ALLOWED_IMAGE_TYPES"
}
EOF

exec "$@"