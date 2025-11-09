# Frontend Application

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 18.

## Theming System

This project includes a flexible theming system that allows users to switch between light and dark modes. The system is designed to be easily extensible.

### How it Works

The theming system is managed by the `ThemeService` located in `src/app/core/services/theme.service.ts`. This service is responsible for:

-   **Toggling Themes**: It provides a method to switch between the available themes.
-   **Persisting User Preference**: The user's selected theme is saved to `localStorage`, so it persists across sessions.
-   **Applying the Theme**: The service applies the current theme by setting a `data-theme` attribute on the `<body>` element of the document.

### Theme Files

The theme variables are defined in SCSS files located in the `src/app/core/theme/` directory.

-   `_variables.scss`: This file contains the CSS variables for both the `light` and `dark` themes. The themes are defined using the `[data-theme="..."]` attribute selector.
-   `styles.scss`: This file imports the variables and can be used to add other global theme styles.

### Adding a New Theme

To add a new theme (e.g., a "blue" theme):

1.  **Define Theme Variables**: Open `src/app/core/theme/_variables.scss` and add a new block for your theme:

    ```scss
    [data-theme="blue"] {
      --primary-color: #005f9e;
      --secondary-color: #00a8e8;
      --background-color: #e6f7ff;
      --text-color: #001f3f;
      --card-background-color: #ffffff;
      --border-color: #cceeff;
    }
    ```

2.  **Update the Theme Service**: If you want to add more than just light/dark, you would need to update the `ThemeService` to handle more theme options.

## Development server

Run `npm start` for a dev server. Navigate to `http://localhost:4400/`. The application will automatically reload if you change any of the source files.

## Project Structure

The folder structure of this project follows the standard Angular best practices, with a few additions for scalability.

- **`src/`**: Contains the source code of the application.
  - **`app/`**: Contains the application logic, components, and modules.
    - **`core/`**: Global services, guards, and interceptors.
    - **`shared/`**: Reusable components, pipes, and directives.
    - **`modules/`**: Main features of the application (e.g., dashboard, users, reports). These are organizational folders, not Angular Modules.
    - **`app.component.ts`**: The root component of the application.
    - **`app.config.ts`**: Global application configuration.
    - **`app.routes.ts`**: Main routing configuration.
  - **`assets/`**: Images, global styles, and fonts.
  - **`environments/`**: Environment-specific configurations (e.g., `environment.ts`, `environment.prod.ts`).
  - **`index.html`**: The main HTML file.
  - **`main.ts`**: The entry point of the application.
  - **`styles.scss`**: Global styles.

---

You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.
## TypeScript Best Practices
- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain
## Angular Best Practices
- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v18+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.
## Accessibility Requirements
- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.
### Components
- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- When using external templates/styles, use paths relative to the component TS file.
## State Management
- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead
## Templates
- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.
- Do not write arrow functions in templates (they are not supported).
- Do not write Regular expressions in templates (they are not supported).
## Services
- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection
