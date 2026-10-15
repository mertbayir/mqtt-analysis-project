# Kaynakça Durumu

Metne yalnızca **"doğrulandı"** olanlar girer. Durum değiştikçe tarihle güncellenir.

## Doğrulandı (doğrudan kaynaktan, 11-12 Ekim 2026)

| # | Künye | Neyi destekliyor | Doğrulama |
|---|---|---|---|
| [1] | I. Fette ve A. Melnikov, "The WebSocket Protocol," RFC 6455, IETF, Aralık 2011. | §7.2.3: ilk yeniden bağlanma rastgele geciktirilmeli ("0 ile 5 saniye arası rastgele değer makul bir ilk gecikme"); sonraki denemeler kesilmiş ikili üstel geri çekilme gibi bir yöntemle giderek uzamalı | Bölüm metni rfc-editor.org'dan okundu. **Yazar adı ve tarih künyesi** ayrıca kontrol edilecek |
| [2] | C. Jennings, R. Mahy ve F. Audet, "Managing Client-Initiated Connections in the Session Initiation Protocol (SIP)," RFC 5626, IETF, Ekim 2009. | §4.5: `W = min(max-time, base-time × 2^ardışık-hata)`; gerçek bekleme W'nin %50-%100'ü arasında düzgün rastgele | Formül ve ifade rfc-editor.org'dan okundu. **Yazar adı ve tarih künyesi** ayrıca kontrol edilecek |

## Yazılım (sürüm kayıtları `00_ortam.md` içinde)

| Öğe | Sürüm | Not |
|---|---|---|
| Eclipse Mosquitto | 2.1.2 | Docker imajı `eclipse-mosquitto:2`, özet `00_ortam.md`'de |
| MQTTnet | 5.2.0.1603 | NuGet |
| .NET | 8.0 | Çalışma zamanı konteyner imajı |
| Docker Engine | 28.4.0 | Docker Desktop, WSL2, çekirdek 5.15.167.4 |

## Doğrulanacak (okunmadan metne girmez)

| Kaynak | Neden gerekli | Not |
|---|---|---|
| **Spillway**, Xie ve ark., ACM SIGCOMM 2026 | En yakın çalışma. Farkımız burada yazılacak | **Henüz okunmadı.** Açık erişim |
| Domingues, Faria, Portugal, EURASIP JWCN 2024 | Biriken mesaj boşaltma, "çok istemcili" gelecek çalışma cümlesi | "Gelecek çalışma" cümlesi kendi gözümüzle görülmeli |
| MQTT 5.0 OASIS standardı | Neden kodları, Session Expiry, standartta "ne kadar sonra gel" alanı olmaması | Künye ve ilgili bölümler doğrulanacak |
| Huang ve ark., OSDI 2022; Isaacs ve ark., HotOS 2025 | Metastabil çöküş, yeniden deneme güçlenmesi | Yalnızca giriş/tartışma okunacak |
| Floyd ve Jacobson, 1994 | Zamanlayıcı senkronizasyonu, rastgelelik ihtiyacı | |
| Tıbbi IoT çalışmaları (02, "Ek (11 Ekim)") | Çerçevenin dayanağı | **Yalnızca arama özeti**, makaleler okunmadı |

## Kullanılmayacak / dikkat

- `15b` ve `15c` dosyalarındaki künyeler bir yapay zekâ ajanının taramasından geliyor. **Tek tek doğrulanmadan kaynakçaya girmez.**
