# Frontend Application

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 18.

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
