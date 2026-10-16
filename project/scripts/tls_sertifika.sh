#!/bin/sh
# Adım 6 (TLS) için ATILABİLİR test sertifikaları üretir (Git Bash: sh scripts/tls_sertifika.sh).
# Çıktı: mosquitto/certs/{ca.crt,server.crt,server.key}. Bu klasör git dışıdır; anahtarlar depoya girmez.
# Sunucu anahtarı RSA-2048 (yaygın varsayılan; el sıkışma maliyeti ECDSA'dan yüksektir, bu çalışmada bilinçli seçim).
set -e
cd "$(dirname "$0")/../mosquitto"
mkdir -p certs && cd certs
export MSYS_NO_PATHCONV=1
openssl req -x509 -newkey rsa:2048 -nodes -keyout ca.key -out ca.crt -days 365 -subj "/CN=mqtt-test-ca"
echo "subjectAltName = IP:172.21.0.2, DNS:broker, DNS:localhost" > san.cnf
openssl req -newkey rsa:2048 -nodes -keyout server.key -out server.csr -subj "/CN=mqtt-broker"
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt -days 365 -extfile san.cnf
echo "tamam: mosquitto/certs/"
