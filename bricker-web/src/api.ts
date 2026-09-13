export const apiUrl =
  import.meta.env.VITE_API_URL ?? "http://localhost:5190/api/v1";
export const hubUrl = `${apiUrl.replace(/\/api\/v1\/?$/, "")}/hubs/chat`;
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
  postalCode?: string;
  street?: string;
  neighborhood?: string;
  addressNumber?: string;
  addressComplement?: string;
  category: string;
  categorySlug: string;
  sellerDisplayName: string;
  imageUrl?: string;
  imageUrls: string[];
  createdAtUtc: string;
  hasConfirmedSale: boolean;
};
export type Profile = {
  id: string;
  displayName: string;
  email: string;
  city?: string;
  state?: string;
  whatsApp?: string;
  requiresProfileCompletion: boolean;
  createdAtUtc: string;
};
export type ListingImage = { id: string; url: string; sortOrder: number };
export type Detail = {
  listing: Listing;
  images: ListingImage[];
  seller?: {
    id?: string;
    displayName: string;
    city?: string;
    state?: string;
    rating?: number;
    reviewCount: number;
    createdAtUtc: string;
  };
};
export type Interest = {
  id: string;
  listingId: string;
  conversationId?: string;
  userId: string;
  listingTitle: string;
  displayName: string;
  createdAtUtc: string;
};
export type InterestCreated = { interestId: string; conversationId: string };
export type InterestConversation = {
  interestId: string;
  conversationId: string;
  direction: "sent" | "received";
  interestCreatedAtUtc: string;
  listingId: string;
  listingTitle: string;
  listingImageUrl?: string;
  listingStatus: number;
  otherUserId: string;
  otherUserDisplayName: string;
  lastMessage?: string;
  lastMessageAtUtc?: string;
  unreadCount: number;
  review?: InterestReviewContext;
};
export type ChatMessage = { id: string; conversationId: string; senderId?: string; senderDisplayName?: string; body: string; createdAtUtc: string; readAtUtc?: string; type: number };
export type UserReview = { id: string; saleId: string; reviewerId: string; reviewerDisplayName: string; revieweeRole: string; listingTitle: string; rating: number; comment?: string; createdAtUtc: string; updatedAtUtc?: string; canEdit: boolean };
export type InterestReviewContext = { saleId: string; status: "pending" | "completed"; revieweeId: string; revieweeDisplayName: string; revieweeRole: string; confirmedAtUtc: string; myReview?: UserReview };
export type PublicUserProfile = { id: string; displayName: string; city?: string; state?: string; createdAtUtc: string; rating?: number; reviewCount: number; reviews: UserReview[]; page: number; pageSize: number; totalCount: number };

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
