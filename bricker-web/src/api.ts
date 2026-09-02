export const apiUrl =
  import.meta.env.VITE_API_URL ?? "http://localhost:5190/api/v1";
export const fileUrl = (path?: string | null) =>
  path ? `http://localhost:5190${path}` : undefined;

export type Category = { id: string; name: string; slug: string };
export type Listing = {
  id: string;
  title: string;
  description: string;
  price: number;
  unit: string;
  quantity: number;
  condition: number;
  status: number;
  city: string;
  state: string;
  category: string;
  categorySlug: string;
  sellerDisplayName: string;
  imageUrl?: string;
  imageUrls: string[];
  createdAtUtc: string;
};
export type Profile = {
  id: string;
  displayName: string;
  email: string;
  city?: string;
  state?: string;
  whatsApp?: string;
  createdAtUtc: string;
};
export type ListingImage = { id: string; url: string; sortOrder: number };
export type Detail = {
  listing: Listing;
  images: ListingImage[];
  seller?: {
    displayName: string;
    city?: string;
    state?: string;
    createdAtUtc: string;
  };
};
export type Interest = {
  id: string;
  listingId: string;
  listingTitle: string;
  displayName: string;
  email: string;
  whatsApp?: string;
  createdAtUtc: string;
};

export async function api<T>(path: string, options: RequestInit = {}) {
  const response = await fetch(`${apiUrl}${path}`, {
    credentials: "include",
    ...options,
    headers:
      options.body instanceof FormData
        ? options.headers
        : { "Content-Type": "application/json", ...options.headers },
  });
  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as {
      message?: string;
      errors?: Record<string, string[]>;
    };
    throw new Error(
      body.message ||
        Object.values(body.errors ?? {})
          .flat()
          .join(" ") ||
        "Não foi possível concluir esta ação.",
    );
  }
  return response.status === 204
    ? (undefined as T)
    : (response.json() as Promise<T>);
}
