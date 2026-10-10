export interface Role {
  id: string;
  name: string;
  description?: string | null;
  systemKey?: string | null;
  isSystem: boolean;
  isActive: boolean;
  permissions: string[];
  userCount: number;
}

export interface SaveRole {
  name: string;
  description?: string | null;
  permissions: string[];
}

export interface PermissionDefinition {
  key: string;
  module: string;
  label: string;
}
