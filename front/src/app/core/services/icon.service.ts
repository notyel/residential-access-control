import { Injectable } from '@angular/core';
import {
  BarChart,
  Calendar,
  Users,
  Home,
  Settings,
  Shield,
  ClipboardList,
  UserPlus,
  Eye,
  Edit,
  Trash2,
  Plus,
  Search,
  Filter,
  Save,
  X,
  Check,
  AlertCircle,
  Info,
  LucideIconData,
} from 'lucide-angular';

@Injectable({
  providedIn: 'root',
})
export class IconService {
  // Registro centralizado de todos los iconos disponibles
  private iconRegistry: Record<string, LucideIconData> = {
    // Iconos de navegación
    BarChart: BarChart,
    Calendar: Calendar,
    Users: Users,
    Home: Home,
    Settings: Settings,
    Shield: Shield,

    // Iconos de acciones
    ClipboardList: ClipboardList,
    UserPlus: UserPlus,
    Eye: Eye,
    Edit: Edit,
    Trash2: Trash2,
    Plus: Plus,
    Search: Search,
    Filter: Filter,
    Save: Save,
    X: X,
    Check: Check,

    // Iconos de estado
    AlertCircle: AlertCircle,
    Info: Info,
  };

  /**
   * Obtiene el icono por su nombre
   * @param iconName Nombre del icono
   * @returns LucideIconData o undefined si no existe
   */
  getIcon(iconName: string): LucideIconData | undefined {
    return this.iconRegistry[iconName];
  }

  /**
   * Verifica si un icono está disponible
   * @param iconName Nombre del icono
   * @returns boolean
   */
  hasIcon(iconName: string): boolean {
    return iconName in this.iconRegistry;
  }

  /**
   * Registra nuevos iconos dinámicamente
   * @param iconName Nombre del icono
   * @param iconData Datos del icono de Lucide
   */
  registerIcon(iconName: string, iconData: LucideIconData): void {
    this.iconRegistry[iconName] = iconData;
  }

  /**
   * Registra múltiples iconos a la vez
   * @param icons Objeto con pares nombre-icono
   */
  registerIcons(icons: Record<string, LucideIconData>): void {
    Object.assign(this.iconRegistry, icons);
  }

  /**
   * Obtiene todos los nombres de iconos disponibles
   * @returns Array de nombres de iconos
   */
  getAvailableIcons(): string[] {
    return Object.keys(this.iconRegistry);
  }

  /**
   * Obtiene todos los iconos de una categoría específica
   * @param category Categoría de iconos (basado en el prefijo del nombre)
   * @returns Objeto con iconos de la categoría
   */
  getIconsByCategory(category: string): Record<string, LucideIconData> {
    const categoryIcons: Record<string, LucideIconData> = {};
    Object.entries(this.iconRegistry).forEach(([name, icon]) => {
      if (name.toLowerCase().includes(category.toLowerCase())) {
        categoryIcons[name] = icon;
      }
    });
    return categoryIcons;
  }
}
