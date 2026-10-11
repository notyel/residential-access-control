# HU-136 · Fallas de la cadena de suministro de software (Software Supply Chain Failures)

**Épica:** E8 · Validación OWASP Top 10 · **Categoría:** Software Supply Chain Failures
**Estado:** Pendiente · **Depende de:** — · **Criterios de la épica:** 1, 2, 8

## Historia

Como evaluador de seguridad, quiero revisar las dependencias del backend y del frontend, la forma
en que se instalan y la automatización de la entrega, para identificar paquetes con riesgos
conocidos, componentes sin uso y fallas de integridad en la construcción.

## Qué se prueba

1. **Inventario de dependencias.** Levantar el listado de paquetes NuGet de los 7 proyectos
   (los `.csproj` de `AccessControl.API`, `Application`, `Persistence`, `Tests`, `DevDb`) y de
   `front/package.json` + `front/package-lock.json`.
2. **Vulnerabilidades conocidas.** Ejecutar `dotnet list package --vulnerable` y `npm audit`
   en el entorno de pruebas y registrar la salida completa como evidencia.
3. **Desactualización.** `dotnet list package --outdated` y `npm outdated`; contrastar con el
   ciclo de soporte de .NET 10 y Angular 18.
4. **Uso real de cada dependencia.** Verificar que cada paquete declarado se use en el código:
   `jwt-decode@^4.0.0` está en `front/package.json:25` pero no se importa en `front/src`
   (el decodificado del JWT se hace a mano en `auth.service.ts:147-157`).
5. **Instalación determinista.** Confirmar que el CI usa `npm ci` con `package-lock.json`
   (`.github/workflows/ci.yml:35`) y que NuGet se restaura con los `.csproj` versionados.
6. **Automatización de revisión.** Verificar si existe Dependabot/Renovate y si el workflow de CI
   ejecuta alguna auditoría: hoy `.github/workflows/ci.yml` solo hace build y test; no hay
   otros workflows ni `.github/dependabot.yml`.
7. **Integridad de acciones de CI.** Comprobar que `actions/checkout@v4`, `actions/setup-dotnet@v4`
   y `actions/setup-node@v4` están referenciadas por tag mutable y no por SHA fijo
   (`ci.yml:15,16,30,31`).
8. **Paquetes duplicados o en conflicto.** `System.IdentityModel.Tokens.Jwt 8.23.0` y
   `Microsoft.IdentityModel.Tokens 8.23.0` conviven con `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12`;
   verificar la versión efectiva resuelta (`dotnet list package --include-transitive`).

## Fundamento en el API

### Lo que sí cumple con la épica

- Stack reciente y soportado: `net10.0`, EF Core `10.0.12`, Npgsql `10.0.3`, JwtBearer `10.0.12`,
  IdentityModel `8.23.0`, `BCrypt.Net-Next 4.0.3`, xUnit `2.9.3`, Angular `^18.2`.
- `package-lock.json` versionado y `npm ci` en CI → instalación reproducible del frontend.
- CI que compila backend y frontend y corre la suite en cada PR y push a `master`/`develop`
  (`.github/workflows/ci.yml`).
- Sin paquetes de terceros de riesgo alto conocido en la lista directa (a confirmar con el
  escaneo del paso 2).

### Lo que no cumple o falta verificar

- No hay escaneo de vulnerabilidades ni desactualización en CI (ni `dotnet list package --vulnerable`,
  ni `npm audit`, ni `npm outdated`).
- No hay Dependabot/Renovate ni política de actualización.
- `jwt-decode` declarado sin uso: dependencia muerta que amplía la superficie.
- Acciones de GitHub referenciadas por tag mutable (riesgo de compromiso de la cadena de
  suministro del propio pipeline).
- No hay SBOM ni registro de versiones exactas de despliegue.
- No hay verificación de integridad de artefactos publicados (hoy el despliegue no publica
  artefactos desde CI; queda por definir).

### Hallazgos, riesgo y recomendación

| # | Hallazgo | Componente | Impacto | Riesgo | Recomendación | Prioridad |
| --- | --- | --- | --- | --- | --- | --- |
| A06-1 | Sin escaneo de dependencias en CI | `.github/workflows/ci.yml` | Vulnerabilidades conocidas entran sin detectarse | Alto | Agregar pasos `dotnet list package --vulnerable --include-transitive` y `npm audit --audit-level=high` que fallen el build | P1 |
| A06-2 | Sin renovación automática de dependencias | `.github/` | Componentes desactualizados se acumulan | Medio | Habilitar Dependabot para NuGet y npm con ventanas semanales | P1 |
| A06-3 | `jwt-decode` sin usar | `front/package.json:25` | Superficie innecesaria; confusión sobre quién valida el token | Bajo | Eliminar la dependencia (la validación de firma es del servidor) | P2 |
| A06-4 | Acciones de CI sin pin por SHA | `ci.yml:15-31` | Compromiso de una action afecta el pipeline completo | Medio | Fijar por SHA con comentario de versión | P2 |
| A06-5 | Sin SBOM ni inventario versionado de despliegue | repositorio | Difícil responder a una CVE contra lo desplegado | Bajo | Generar SBOM (CycloneDX) en CI y adjuntarlo al informe | P3 |

## Criterios de aceptación

- [ ] Documentado el inventario de dependencias backend y frontend con versiones exactas
      (criterio 1).
- [ ] Registrada la salida de `dotnet list package --vulnerable` y `npm audit`, con los hallazgos
      traducidos a componente, impacto, riesgo, recomendación y prioridad (criterios 1, 2 y 8).
- [ ] Documentado si el pipeline de entrega es reproducible e íntegro (lockfile, pin de actions,
      pasos de auditoría) y qué falta.
- [ ] Cada paquete declarado justifica su uso o se marca para retiro.

## Verificación

La evidencia es la salida en texto de los comandos de escaneo, guardada en
`documentation/owasp-top10/evidencias/A06-<n>.txt`, junto con la versión de las herramientas
usadas. Los hallazgos que no se puedan escanear en este entorno se marcan como "por confirmar"
con la instrucción exacta para reproducirlos.
