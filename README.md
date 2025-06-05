# Residential Access Control

Solución para gestionar el ingreso y salida de personas en conjuntos residenciales o comunidades cerradas. Incluye una API desarrollada con .NET 8 y un frontend web (en desarrollo). Diseñado para ser usado en porterías o sistemas automatizados de vigilancia.

## Características

- Registro de residentes, visitantes, empleados y proveedores.
- Historial centralizado de accesos.
- API RESTful construida con .NET 8.
- Preparado para integración con Nginx y despliegue en entornos Linux.

## Tecnologías

- **Backend:** .NET 8 Web API
- **Base de datos:** SQLite
- **Frontend:** (en desarrollo)
- **Despliegue:** Linux con Nginx y systemd

## Uso básico

```bash
# Publicar en modo Release
dotnet publish AccessControl.API/AccessControl.API.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
````

```bash
# Ejecutar la API
cd ./publish
./AccessControl.API
```

## Configuración Nginx (opcional)

```nginx
server {
    listen 80;
    server_name 192.168.1.17;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```


