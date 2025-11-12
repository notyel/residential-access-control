# Sistema de Estilos - Botones e Iconos ✨

## 📁 Estructura de Archivos

```
src/app/core/theme/
├── _base.scss          # Estilos base del sistema
├── _buttons.scss       # Sistema completo de botones ⭐
├── _icons.scss         # Sistema de iconos y alineación
├── _variables.scss     # Variables CSS
├── _reset.scss         # Reset CSS
└── styles.scss         # Archivo principal de importación
```

## 🎯 **Nueva Arquitectura - Solo Botones Primarios con Fondo**

### ✅ **Principio de Diseño**

- **Solo botones primarios** tienen fondo coloreado (como el botón de login)
- **Botones secundarios, texto e icono** mantienen tipografía, espaciado y efectos consistentes **SIN fondo**
- **Alineación perfecta** de iconos en todos los tipos de botón

## 🔧 Sistema de Botones

### **🎨 Botones Primarios (CON FONDO COLOREADO)**

```html
<button mat-flat-button color="primary">
  <lucide-icon [img]="PlusIcon"></lucide-icon>
  Agregar elemento
</button>
```

**Características:**

- ✅ **Fondo**: `var(--primary-color)` (color del botón de login)
- ✅ **Texto**: Blanco
- ✅ **Altura**: 48px mínimo
- ✅ **Border radius**: 8px
- ✅ **Gap automático**: 8px entre icono y texto
- ✅ **Hover**: brightness(0.9) + translateY(-1px) + shadow
- ✅ **Uso**: Acciones principales (Guardar, Agregar, Enviar)

### **📝 Botones Secundarios (SIN FONDO, CON BORDE)**

```html
<button mat-outlined-button>
  <lucide-icon [img]="EditIcon"></lucide-icon>
  Editar
</button>
```

**Características:**

- ✅ **Fondo**: Transparente
- ✅ **Borde**: `var(--primary-color)`
- ✅ **Texto**: `var(--primary-color)`
- ✅ **Misma tipografía y espaciado** que botones primarios
- ✅ **Hover**: Fondo sutil rgba(0, 123, 255, 0.08) + transformación
- ✅ **Uso**: Acciones secundarias (Editar, Cancelar, Ver más)

### **💬 Botones de Texto (SIN FONDO, SIN BORDE)**

```html
<button mat-button>
  <lucide-icon [img]="InfoIcon"></lucide-icon>
  Información
</button>
```

**Características:**

- ✅ **Fondo**: Transparente
- ✅ **Borde**: Ninguno
- ✅ **Texto**: `var(--primary-color)`
- ✅ **Misma tipografía y espaciado** que otros botones
- ✅ **Hover**: Fondo muy sutil rgba(0, 123, 255, 0.04) + transformación
- ✅ **Uso**: Acciones terciarias (Enlaces, Información, Ayuda)

### **🔘 Botones de Icono (SOLO ICONO)**

```html
<button mat-icon-button color="primary">
  <lucide-icon [img]="MenuIcon"></lucide-icon>
</button>
```

**Características:**

- ✅ **Fondo**: Transparente
- ✅ **Tamaño**: 40x40px
- ✅ **Color**: `var(--primary-color)` cuando se especifica
- ✅ **Consistencia tipográfica** con otros botones
- ✅ **Uso**: Menús, toggles, acciones rápidas

## 🎨 Sistema de Iconos

### **Alineación Perfecta Garantizada**

- **Flexbox centralizado**: `align-items: center` + `justify-content: center`
- **Gap automático**: 8px entre icono y texto (no más `mr-2` manual)
- **Tamaños específicos por contexto**:
  - Botones: 18px × 18px
  - Navegación: 20px × 20px
  - Formularios: 20px × 20px
- **Sin márgenes problemáticos**: El sistema anula automáticamente clases conflictivas

### **Tamaños por Contexto**

```scss
// Botones normales
lucide-icon {
  width: 18px;
  height: 18px;
}

// Botones de icono
.mat-mdc-icon-button lucide-icon {
  width: 20px;
  height: 20px;
}

// Navegación
.nav-link lucide-icon {
  width: 20px;
  height: 20px;
}
```

## 🚀 Beneficios de la Nueva Implementación

### ✅ **Jerarquía Visual Clara**

- **Primarios**: Destacan con fondo coloreado para acciones críticas
- **Secundarios**: Visibles pero no dominantes con borde
- **Texto**: Discretos para acciones opcionales
- **Icono**: Compactos para espacios reducidos

### ✅ **Consistencia Tipográfica**

- **Misma fuente**: font-weight: 500 en todos los botones
- **Misma altura**: 48px mínimo para accesibilidad WCAG
- **Mismo espaciado**: Gap de 8px automático
- **Mismos efectos**: Transform y hover consistentes

### ✅ **Alineación Perfecta**

- **Sin márgenes manuales**: El sistema maneja todo automáticamente
- **Flexbox perfecto**: Centrado horizontal y vertical garantizado
- **Responsive**: Funciona en todos los tamaños de pantalla

## 📋 Guía de Migración Actualizada

### **✅ CORRECTO (Nueva implementación)**

```html
<!-- Botón primario (acción principal) -->
<button mat-flat-button color="primary">
  <lucide-icon [img]="SaveIcon"></lucide-icon>
  Guardar
</button>

<!-- Botón secundario (acción secundaria) -->
<button mat-outlined-button>
  <lucide-icon [img]="EditIcon"></lucide-icon>
  Editar
</button>

<!-- Botón de texto (acción terciaria) -->
<button mat-button>
  <lucide-icon [img]="InfoIcon"></lucide-icon>
  Más información
</button>
```

### **❌ ANTERIOR (Problemático)**

```html
<!-- Todos tenían el mismo aspecto visual -->
<button mat-flat-button color="primary">
  <lucide-icon [img]="EditIcon" class="mr-2"></lucide-icon>
  Editar
</button>
```

### **Cambios Clave**

1. ✅ **Solo primarios con `color="primary"`** para fondo coloreado
2. ✅ **Secundarios sin `color`** para mantener borde sin fondo
3. ❌ **Remover clases** `mr-2`, `ml-2`, `mx-2` de iconos
4. ✅ **Confiar en el gap automático** de 8px
5. ✅ **Usar jerarquía visual** apropiada por tipo de acción

## 🔍 Casos de Uso por Tipo

### **Botones Primarios** (`mat-flat-button color="primary"`)

- Guardar formulario
- Agregar nuevo elemento
- Confirmar acción crítica
- Enviar datos
- Registrar visita

### **Botones Secundarios** (`mat-outlined-button`)

- Editar elemento existente
- Ver detalles
- Filtrar contenido
- Cancelar (sin destruir datos)
- Exportar datos

### **Botones de Texto** (`mat-button`)

- Enlaces internos
- Mostrar más información
- Ayuda contextual
- Acciones opcionales
- Navegación suplementaria

### **Botones de Icono** (`mat-icon-button`)

- Menú hamburguesa
- Toggle de tema
- Acciones rápidas en tablas
- Navegación compacta
- Controles multimedia

¡El sistema ahora tiene **jerarquía visual clara**, **alineación perfecta** y **consistencia tipográfica** en todos los botones! 🎉
