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

Run `npm start` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

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
