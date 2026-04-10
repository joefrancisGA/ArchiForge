/** Mirrors ArchLucid.Core.Pagination.PagedResponse (camelCase JSON). */
export type PagedResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
};
