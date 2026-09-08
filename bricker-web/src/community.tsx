import { HubConnectionBuilder, type HubConnection } from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  api,
  fileUrl,
  hubUrl,
  type ChatMessage,
  type ConversationSummary,
  type PendingReview,
  type Profile,
  type PublicUserProfile,
  type UserReview,
} from "./api";

const dateTime = (value?: string) =>
  value
    ? new Intl.DateTimeFormat("pt-BR", {
        dateStyle: "short",
        timeStyle: "short",
      }).format(new Date(value))
    : "";

const connectHub = () =>
  new HubConnectionBuilder()
    .withUrl(hubUrl, { withCredentials: true })
    .withAutomaticReconnect()
    .build();

export function CompleteProfilePage({
  profile,
  setProfile,
}: {
  profile: Profile | null;
  setProfile: (profile: Profile) => void;
}) {
  const navigate = useNavigate();
  const [city, setCity] = useState(profile?.city ?? "");
  const [state, setState] = useState(profile?.state ?? "");
  const [error, setError] = useState("");
  if (!profile) return <Navigate to="/entrar" />;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    try {
      const updated = await api<Profile>("/profile", {
        method: "PUT",
        body: JSON.stringify({
          displayName: profile.displayName,
          city: city.trim(),
          state: state.trim().toUpperCase(),
          whatsApp: profile.whatsApp,
        }),
      });
      setProfile(updated);
      navigate("/", { replace: true });
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Não foi possível completar o perfil.");
    }
  };

  return (
    <main className="page auth complete-profile">
      <p className="eyebrow">ÚLTIMO PASSO</p>
      <h1>Complete seu perfil</h1>
      <p className="page-intro">
        Olá, {profile.displayName}. Informe sua localização para encontrarmos materiais perto de você.
      </p>
      <form onSubmit={submit}>
        <div className="two">
          <label>
            Cidade
            <input required minLength={2} maxLength={100} value={city} onChange={(event) => setCity(event.target.value)} />
          </label>
          <label>
            UF
            <input required minLength={2} maxLength={2} pattern="[A-Za-z]{2}" value={state} onChange={(event) => setState(event.target.value.toUpperCase())} />
          </label>
        </div>
        {error && <p className="form-error">{error}</p>}
        <button className="primary-button full">Começar a usar a Bricker</button>
      </form>
    </main>
  );
}

export function ConversationsPage({ profile }: { profile: Profile | null }) {
  const [items, setItems] = useState<ConversationSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const load = useCallback(() => {
    if (!profile) return;
    void api<ConversationSummary[]>("/conversations")
      .then(setItems)
      .finally(() => setLoading(false));
  }, [profile]);

  useEffect(() => {
    load();
    if (!profile) return;
    const connection = connectHub();
    connection.on("ConversationUpdated", load);
    void connection.start().catch(() => undefined);
    return () => void connection.stop();
  }, [load, profile]);
  if (!profile) return <Navigate to="/entrar" />;

  return (
    <main className="page conversations-page">
      <p className="eyebrow">NEGOCIAÇÕES</p>
      <h1>Conversas</h1>
      {loading ? (
        <p>Carregando conversas...</p>
      ) : items.length ? (
        <div className="conversation-list">
          {items.map((item) => (
            <Link to={`/conversas/${item.id}`} className="conversation-row" key={item.id}>
              <div className="conversation-image">
                {item.listingImageUrl ? <img src={fileUrl(item.listingImageUrl)} alt="" /> : <span>Sem foto</span>}
              </div>
              <div className="conversation-copy">
                <div><strong>{item.otherUserDisplayName}</strong><time>{dateTime(item.lastMessageAtUtc)}</time></div>
                <span>{item.listingTitle}</span>
                <p>{item.lastMessage ?? "Conversa iniciada. Envie a primeira mensagem."}</p>
              </div>
              {item.unreadCount > 0 && <b className="unread-badge">{item.unreadCount}</b>}
            </Link>
          ))}
        </div>
      ) : (
        <div className="empty">Suas conversas aparecerão aqui quando você demonstrar interesse em um material.</div>
      )}
    </main>
  );
}

export function ConversationPage({ profile }: { profile: Profile | null }) {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const [summary, setSummary] = useState<ConversationSummary | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [body, setBody] = useState("");
  const [error, setError] = useState("");
  const [hasOlder, setHasOlder] = useState(false);
  const connectionRef = useRef<HubConnection | null>(null);
  const endRef = useRef<HTMLDivElement | null>(null);

  const markRead = useCallback(() => {
    if (!id) return;
    void api<void>(`/conversations/${id}/read`, { method: "POST" }).then(() =>
      window.dispatchEvent(new Event("bricker:unread-changed")),
    );
  }, [id]);

  useEffect(() => {
    if (!profile) return;
    void Promise.all([
      api<ConversationSummary[]>("/conversations"),
      api<ChatMessage[]>(`/conversations/${id}/messages?pageSize=50`),
    ])
      .then(([conversations, initialMessages]) => {
        const found = conversations.find((item) => item.id === id);
        if (!found) return navigate("/conversas", { replace: true });
        setSummary(found);
        setMessages(initialMessages);
        setHasOlder(initialMessages.length === 50);
        markRead();
      })
      .catch(() => navigate("/conversas", { replace: true }));

    const connection = connectHub();
    connectionRef.current = connection;
    connection.on("MessageReceived", (message: ChatMessage) => {
      if (message.conversationId !== id) return;
      setMessages((current) => current.some((item) => item.id === message.id) ? current : [...current, message]);
      if (message.senderId !== profile.id) markRead();
    });
    connection.on("MessagesRead", (payload: { conversationId: string; readerId: string; readAtUtc: string }) => {
      if (payload.conversationId !== id || payload.readerId === profile.id) return;
      setMessages((current) => current.map((message) =>
        message.senderId === profile.id && !message.readAtUtc ? { ...message, readAtUtc: payload.readAtUtc } : message,
      ));
    });
    connection.onreconnected(() => connection.invoke("JoinConversation", id));
    void connection.start().then(() => connection.invoke("JoinConversation", id)).catch(() => setError("O chat em tempo real está se reconectando."));
    return () => {
      connectionRef.current = null;
      void connection.stop();
    };
  }, [id, markRead, navigate, profile]);

  useEffect(() => endRef.current?.scrollIntoView({ behavior: "smooth" }), [messages.length]);
  if (!profile) return <Navigate to="/entrar" />;

  const send = async (event: FormEvent) => {
    event.preventDefault();
    const value = body.trim();
    if (!value) return;
    setError("");
    try {
      const sent = await api<ChatMessage>(`/conversations/${id}/messages`, {
        method: "POST",
        body: JSON.stringify({ body: value }),
      });
      setMessages((current) => current.some((item) => item.id === sent.id) ? current : [...current, sent]);
      setBody("");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Não foi possível enviar a mensagem.");
    }
  };

  const loadOlder = async () => {
    const first = messages[0];
    if (!first) return;
    const older = await api<ChatMessage[]>(`/conversations/${id}/messages?pageSize=50&before=${encodeURIComponent(first.createdAtUtc)}`);
    setMessages((current) => [...older, ...current]);
    setHasOlder(older.length === 50);
  };

  return (
    <main className="page chat-page">
      <Link className="back" to="/conversas">← Voltar às conversas</Link>
      <section className="chat-shell">
        <header className="chat-header">
          <div>
            <span>CONVERSA COM</span>
            <h1>{summary?.otherUserDisplayName ?? "Carregando..."}</h1>
            {summary && <Link to={`/materiais/${summary.listingId}`}>{summary.listingTitle} →</Link>}
          </div>
          {summary && <Link className="secondary-button" to={`/usuarios/${summary.otherUserId}`}>Ver perfil</Link>}
        </header>
        <div className="message-history">
          {hasOlder && <button className="link-button load-older" onClick={() => void loadOlder()}>Carregar mensagens anteriores</button>}
          {!messages.length && <p className="chat-empty">Apresente-se e combine os detalhes da negociação.</p>}
          {messages.map((message) => {
            const mine = message.senderId === profile.id;
            return (
              <article className={mine ? "message mine" : "message"} key={message.id}>
                <p>{message.body}</p>
                <small>{dateTime(message.createdAtUtc)}{mine ? message.readAtUtc ? " · Lida" : " · Enviada" : ""}</small>
              </article>
            );
          })}
          <div ref={endRef} />
        </div>
        <form className="message-composer" onSubmit={send}>
          <textarea aria-label="Mensagem" maxLength={2000} placeholder="Escreva uma mensagem..." value={body} onChange={(event) => setBody(event.target.value)} />
          <div><span>{body.length}/2.000</span><button className="primary-button" disabled={!body.trim()}>Enviar</button></div>
          {error && <p className="form-error">{error}</p>}
        </form>
      </section>
    </main>
  );
}

function Stars({ value }: { value: number }) {
  return <span className="stars" aria-label={`${value} de 5 estrelas`}>{"★".repeat(value)}{"☆".repeat(5 - value)}</span>;
}

function ReviewForm({ pending, onCreated }: { pending: PendingReview; onCreated: () => void }) {
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [error, setError] = useState("");
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      await api<UserReview>(`/sales/${pending.saleId}/reviews`, {
        method: "POST",
        body: JSON.stringify({ rating, comment }),
      });
      onCreated();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Não foi possível publicar a avaliação.");
    }
  };
  return (
    <form className="review-form" onSubmit={submit}>
      <div><strong>{pending.listingTitle}</strong><span>Avalie {pending.revieweeDisplayName} como {pending.revieweeRole.toLowerCase()}</span></div>
      <label>Nota<select value={rating} onChange={(event) => setRating(Number(event.target.value))}>{[5, 4, 3, 2, 1].map((number) => <option key={number} value={number}>{number} estrela{number > 1 ? "s" : ""}</option>)}</select></label>
      <label>Comentário opcional<textarea maxLength={1000} value={comment} onChange={(event) => setComment(event.target.value)} /></label>
      {error && <p className="form-error">{error}</p>}
      <button className="primary-button">Publicar avaliação</button>
    </form>
  );
}

export function PendingReviewsPanel() {
  const [items, setItems] = useState<PendingReview[]>([]);
  useEffect(() => { void api<PendingReview[]>("/sales/reviews/pending").then(setItems); }, []);
  if (!items.length) return null;
  return (
    <section className="profile-section">
      <div className="profile-section-heading"><span className="section-kicker">REPUTAÇÃO</span><h2>Avaliações pendentes</h2></div>
      <div className="pending-reviews">{items.map((item) => <ReviewForm key={item.saleId} pending={item} onCreated={() => setItems((current) => current.filter((entry) => entry.saleId !== item.saleId))} />)}</div>
    </section>
  );
}

function EditableReview({ review }: { review: UserReview }) {
  const [editing, setEditing] = useState(false);
  const [current, setCurrent] = useState(review);
  const [rating, setRating] = useState(review.rating);
  const [comment, setComment] = useState(review.comment ?? "");
  const save = async (event: FormEvent) => {
    event.preventDefault();
    const updated = await api<UserReview>(`/reviews/${review.id}`, { method: "PUT", body: JSON.stringify({ rating, comment }) });
    setCurrent(updated);
    setEditing(false);
  };
  return (
    <article className="public-review">
      <div><strong>{current.reviewerDisplayName}</strong><Stars value={current.rating} /></div>
      <span>Negociação: {current.listingTitle} · avaliado como {current.revieweeRole.toLowerCase()}</span>
      {editing ? (
        <form onSubmit={save}><select value={rating} onChange={(event) => setRating(Number(event.target.value))}>{[5,4,3,2,1].map((number) => <option key={number}>{number}</option>)}</select><textarea maxLength={1000} value={comment} onChange={(event) => setComment(event.target.value)} /><div><button className="secondary-button" type="button" onClick={() => setEditing(false)}>Cancelar</button><button className="primary-button">Salvar</button></div></form>
      ) : (
        <><p>{current.comment || "Avaliação sem comentário."}</p><small>{dateTime(current.createdAtUtc)}</small>{current.canEdit && <button className="link-button" onClick={() => setEditing(true)}>Editar minha avaliação</button>}</>
      )}
    </article>
  );
}

export function PublicUserPage() {
  const { id = "" } = useParams();
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page") ?? 1));
  const [user, setUser] = useState<PublicUserProfile | null>(null);
  useEffect(() => { void api<PublicUserProfile>(`/users/${id}?page=${page}&pageSize=10`).then(setUser); }, [id, page]);
  if (!user) return <main className="page">Carregando perfil...</main>;
  const pages = Math.max(1, Math.ceil(user.totalCount / user.pageSize));
  return (
    <main className="page public-profile-page">
      <section className="public-profile-hero"><div className="profile-avatar">{user.displayName.charAt(0).toUpperCase()}</div><div><p className="eyebrow">PERFIL NA BRICKER</p><h1>{user.displayName}</h1><p>{user.city || "Cidade não informada"}{user.state ? `, ${user.state}` : ""} · membro desde {new Date(user.createdAtUtc).getFullYear()}</p></div><div className="rating-summary"><strong>{user.rating?.toFixed(1).replace(".", ",") ?? "—"}</strong><Stars value={Math.round(user.rating ?? 0)} /><span>{user.reviewCount} avaliação{user.reviewCount === 1 ? "" : "ões"}</span></div></section>
      <section className="public-reviews"><h2>Avaliações recebidas</h2>{user.reviews.length ? user.reviews.map((review) => <EditableReview key={review.id} review={review} />) : <div className="empty">Este usuário ainda não recebeu avaliações.</div>}</section>
      {pages > 1 && <nav className="pagination"><button disabled={page === 1} onClick={() => setParams({ page: String(page - 1) })}>← Anterior</button><span>Página {page} de {pages}</span><button disabled={page === pages} onClick={() => setParams({ page: String(page + 1) })}>Próxima →</button></nav>}
    </main>
  );
}
