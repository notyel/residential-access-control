# Sistema de Iconos Dinámicos

Este sistema permite cargar y usar iconos de Lucide de manera dinámica en toda la aplicación.

## 🎯 Características

- **Carga dinámica**: Los iconos se cargan solo cuando se necesitan
- **Gestión centralizada**: Todos los iconos están registrados en un servicio central
- **Extensibilidad**: Fácil agregar nuevos iconos sin modificar componentes
- **Fallback**: Icono de respaldo si no se encuentra el solicitado
- **Tipado**: TypeScript completo con autocompletado

## 📁 Estructura

```
src/app/
├── core/
│   └── services/
│       ├── icon.service.ts          # Servicio principal de iconos
│       └── menu.service.ts          # Servicio de menú con iconos dinámicos
└── shared/
    └── components/
        └── dynamic-icon/
            └── dynamic-icon.component.ts  # Componente reutilizable (opcional)
```

## 🚀 Uso Básico

### 1. En el Servicio de Menú

```typescript
// menu.service.ts
const menuItems = [
  {
    name: "Tablero",
    path: "/dashboard",
    icon: "BarChart", // ← Solo el nombre del icono
  },
  {
    name: "Usuarios",
    path: "/users",
    icon: "Users", // ← Se resuelve dinámicamente
  },
];
```

### 2. En el Componente Sidebar

```typescript
// sidebar.component.ts
export class SidebarComponent {
  iconService = inject(IconService);

  getMenuIcon(iconName: string) {
    return this.iconService.getIcon(iconName);
  }
}
```

```html
<!-- sidebar.component.html -->
@for (item of menuItems; track item.path) {
<a [routerLink]="item.path">
  <lucide-icon [img]="getMenuIcon(item.icon)" [size]="20"></lucide-icon>
  <span>{{ item.name }}</span>
</a>
}
```

## 🔧 Configuración

### Agregar Nuevos Iconos

```typescript
// icon.service.ts
import { NewIcon, AnotherIcon } from "lucide-angular";

// En el constructor o método de inicialización
this.iconRegistry = {
  ...this.iconRegistry,
  NewIcon: NewIcon,
  AnotherIcon: AnotherIcon,
};
```

### Registrar Iconos Dinámicamente

```typescript
// En cualquier parte de la aplicación
constructor(private iconService: IconService) {
  // Registrar un solo icono
  this.iconService.registerIcon('CustomIcon', SomeIcon);

  // Registrar múltiples iconos
  this.iconService.registerIcons({
    'Icon1': Icon1,
    'Icon2': Icon2,
    'Icon3': Icon3,
  });
}
```

## 🎨 Ejemplos Avanzados

### Usando el Componente Dynamic Icon

```html
<!-- Usando nombre de icono -->
<app-dynamic-icon iconName="Users" [size]="24" color="#3b82f6"> </app-dynamic-icon>

<!-- Usando objeto de icono directamente -->
<app-dynamic-icon [iconData]="myIconObject" [size]="32" cssClass="my-custom-class"> </app-dynamic-icon>
```

### Verificar Disponibilidad

```typescript
export class MyComponent {
  iconService = inject(IconService);

  checkIcon(iconName: string): boolean {
    return this.iconService.hasIcon(iconName);
  }

  getAvailableIcons(): string[] {
    return this.iconService.getAvailableIcons();
  }
}
```

## 📋 Iconos Disponibles

### Navegación y Dashboard

- `BarChart` - Gráficos y estadísticas
- `Calendar` - Calendario y fechas
- `Users` - Usuarios y grupos
- `Home` - Inicio
- `Settings` - Configuración
- `Shield` - Seguridad

### Acciones CRUD

- `Plus` - Agregar/Crear
- `Edit` - Editar
- `Trash2` - Eliminar
- `Eye` - Ver/Visualizar
- `Search` - Buscar
- `Filter` - Filtrar
- `Save` - Guardar

### Estado y Notificaciones

- `Check` - Éxito/Completado
- `X` - Error/Cerrar
- `AlertCircle` - Advertencia
- `Info` - Información
- `Bell` - Notificaciones

## 🔍 Troubleshooting

### Icono No Aparece

1. **Verificar registro**: Asegúrate de que el icono está registrado en `IconService`
2. **Importación**: Verifica que el icono esté importado de `lucide-angular`
3. **Nombre**: Confirma que el nombre coincide exactamente
4. **Fallback**: Si no aparece nada, revisa el icono de fallback

### Errores de TypeScript

```typescript
// ✅ Correcto
getMenuIcon(iconName: string): LucideIconData | undefined {
  return this.iconService.getIcon(iconName);
}

// ❌ Incorrecto - puede ser undefined
getMenuIcon(iconName: string): LucideIconData {
  return this.iconService.getIcon(iconName); // Error!
}
```

## 🚀 Ventajas del Sistema

1. **Performance**: Solo se cargan los iconos necesarios
2. **Mantenibilidad**: Gestión centralizada de iconos
3. **Escalabilidad**: Fácil agregar nuevos iconos
4. **Flexibilidad**: Soporte para iconos dinámicos desde API
5. **Consistencia**: Misma API para todos los iconos
6. **Type Safety**: TypeScript completo

## 📝 Próximas Mejoras

- [ ] Cache de iconos para mejor performance
- [ ] Lazy loading de iconos por módulos
- [ ] Soporte para iconos SVG personalizados
- [ ] Generación automática de tipos para iconos
- [ ] Herramientas de desarrollo para gestión de iconos
