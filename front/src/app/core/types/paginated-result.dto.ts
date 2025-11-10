export interface PaginatedResultDto<T> {
  items: T[];
  totalCount: number;
}
