# Sistema de Badges

Los badges forman parte del sistema de diseño global de la aplicación y se pueden usar en cualquier componente.

## Uso Básico

```html
<!-- Badge básico -->
<span class="badge badge-primary">Primario</span>

<!-- Badge con diferentes variantes de color -->
<span class="badge badge-success">Éxito</span>
<span class="badge badge-warning">Advertencia</span>
<span class="badge badge-danger">Peligro</span>
<span class="badge badge-secondary">Secundario</span>
<span class="badge badge-info">Información</span>
```

## Variantes de Tamaño

```html
<!-- Badge pequeño -->
<span class="badge badge-small badge-primary">Pequeño</span>

<!-- Badge normal (por defecto) -->
<span class="badge badge-primary">Normal</span>

<!-- Badge grande -->
<span class="badge badge-large badge-primary">Grande</span>
```

## Badge con Iconos

```html
<!-- Badge con icono -->
<span class="badge badge-with-icon badge-success">
  <lucide-icon img="check" class="badge-icon"></lucide-icon>
  Completado
</span>
```

## Badges Outline

```html
<!-- Badge outline -->
<span class="badge badge-outline badge-primary">Outline</span>
<span class="badge badge-outline badge-success">Outline Éxito</span>
```

## Badges Especiales

```html
<!-- Badge contador -->
<span class="badge badge-counter badge-danger">5</span>

<!-- Badge con punto de notificación -->
<span class="badge badge-dot badge-primary">Notificación</span>

<!-- Badge claro con borde -->
<span class="badge badge-light">Claro</span>
```

## Ejemplos en Angular

```typescript
@Component({
  selector: "app-ejemplo",
  template: `
    <!-- En un listado -->
    @for (item of items; track item.id) {
    <div class="item">
      <span>{{ item.name }}</span>
      <span class="badge" [ngClass]="getBadgeClass(item.status)">
        {{ item.status }}
      </span>
    </div>
    }

    <!-- En el page-header -->
    <app-page-header title="Dashboard" subtitle="Panel de control" [showBadge]="true" badgeText="En línea" badgeColor="success"> </app-page-header>
  `,
})
export class EjemploComponent {
  getBadgeClass(status: string): string {
    const statusMap = {
      active: "badge-success",
      pending: "badge-warning",
      inactive: "badge-secondary",
      error: "badge-danger",
    };
    return `badge ${statusMap[status] || "badge-secondary"}`;
  }
}
```

## Colores Disponibles

- `badge-primary`: Azul principal de la aplicación
- `badge-success`: Verde para estados exitosos
- `badge-warning`: Naranja para advertencias
- `badge-danger`: Rojo para errores o estados críticos
- `badge-secondary`: Gris para estados neutros
- `badge-info`: Azul claro para información
- `badge-light`: Claro con borde para estados suaves
- `badge-dark`: Oscuro para contraste

## Notas de Implementación

- Los estilos están definidos en `src/app/core/theme/_badges.scss`
- **Todos los colores utilizan variables CSS** definidas en `_variables.scss` para consistencia del tema
- Se importan automáticamente en el tema global
- Incluyen animaciones de hover y transiciones suaves
- Son completamente responsivos
- Soportan iconos usando el componente `lucide-icon`
- **Compatible con temas claro y oscuro** automáticamente

## Variables CSS Utilizadas

Los badges utilizan las siguientes variables CSS del sistema de diseño:

### Colores Principales

- `--primary-color` y `--accent-color`
- `--success-color` y `--success-light-color`
- `--warning-color` y `--warning-light-color`
- `--danger-color` y `--danger-light-color`
- `--info-color` y `--info-light-color`
- `--secondary-color` y `--secondary-light-color`

### Colores Neutros

- `--light-color` y `--light-secondary-color`
- `--dark-color` y `--dark-secondary-color`

### Texto y Bordes

- `--text-primary` para texto en badges claros
- `--border-color` para bordes
