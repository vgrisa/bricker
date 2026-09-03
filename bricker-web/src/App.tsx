import { useEffect, useState, type FormEvent } from "react";
import {
  Link,
  NavLink,
  Navigate,
  Route,
  Routes,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router-dom";
import {
  api,
  fileUrl,
  type Category,
  type Detail,
  type Interest,
  type Listing,
  type Profile,
  type SentInterest,
} from "./api";
import "./App.css";

const money = (value: number) =>
  value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const condition = (value: number) =>
  ["Ótimo estado", "Bom estado", "Estado regular"][value] ?? "Não informado";
const listingStatus = (value: number) =>
  ["Rascunho", "Disponível", "Reservado", "Vendido", "Inativo"][value] ??
  "Não informado";
const onlyDigits = (value: string) => value.replace(/\D/g, "");
const formatPostalCode = (value: string) => {
  const digits = onlyDigits(value).slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
};
const formatPhone = (value: string) => {
  const digits = onlyDigits(value).slice(0, 11);
  if (digits.length <= 2) return digits;
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10)
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
};

function Layout({
  profile,
  setProfile,
  children,
}: {
  profile: Profile | null;
  setProfile: (profile: Profile | null) => void;
  children: React.ReactNode;
}) {
  const navigate = useNavigate();
  const logout = async () => {
    await api<void>("/auth/logout", { method: "POST" });
    setProfile(null);
    navigate("/");
  };
  return (
    <>
      <header className="site-header">
        <Link className="brand" to="/">
          bricker<span>.</span>
        </Link>
        <nav>
          <NavLink to="/materiais">Materiais</NavLink>
          <NavLink to="/anunciar">Anunciar</NavLink>
          {profile && <NavLink to="/perfil">Meu perfil</NavLink>}
        </nav>
        <div>
          {profile ? (
            <>
              <span className="greeting">
                Olá, {profile.displayName.split(" ")[0]}
              </span>
              <button className="link-button" onClick={() => void logout()}>
                Sair
              </button>
            </>
          ) : (
            <Link className="link-button" to="/entrar">
              Entrar
            </Link>
          )}
          <Link className="primary-button" to="/anunciar">
            Anunciar material
          </Link>
        </div>
      </header>
      {children}
      <footer>bricker. Construção circular, de ponta a ponta.</footer>
    </>
  );
}

function Card({ item, favorite, onFavorite }: { item: Listing; favorite?: boolean; onFavorite?: (item: Listing) => void }) {
  const images = item.imageUrls?.length
    ? item.imageUrls
    : item.imageUrl
      ? [item.imageUrl]
      : [];
  const [activeImage, setActiveImage] = useState(0);
  const changeImage = (direction: number) => {
    setActiveImage(
      (current) => (current + direction + images.length) % images.length,
    );
  };
  return (
    <article className="card">
      {onFavorite && <button className={favorite ? "favorite-button selected" : "favorite-button"} type="button" aria-label={favorite ? "Remover dos favoritos" : "Adicionar aos favoritos"} onClick={() => onFavorite(item)}>{favorite ? "♥" : "♡"}</button>}
      <Link to={`/materiais/${item.id}`} className="card-image-link">
        <div className="card-image">
          {images[activeImage] ? (
            <img src={fileUrl(images[activeImage])} alt={item.title} />
          ) : (
            <span className="image-placeholder">Sem foto</span>
          )}
          <span className="card-category">{item.category}</span>
        </div>
      </Link>
      {images.length > 1 && (
        <div className="card-carousel-controls">
          <button
            type="button"
            aria-label="Foto anterior"
            onClick={() => changeImage(-1)}
          >
            ‹
          </button>
          <button
            type="button"
            aria-label="Próxima foto"
            onClick={() => changeImage(1)}
          >
            ›
          </button>
        </div>
      )}
      <Link to={`/materiais/${item.id}`} className="card-body">
        <small>{condition(item.condition).toUpperCase()}</small>
        <h3>{item.title}</h3>
        <strong>
          {money(item.price)} <em>por {item.unit}</em>
        </strong>
        <p>
          {item.quantity} {item.unit} · {item.neighborhood ? `${item.neighborhood}, ` : ""}{item.city}, {item.state}
        </p>
      </Link>
    </article>
  );
}

function Home() {
  const [recent, setRecent] = useState<Listing[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [total, setTotal] = useState(0);
  useEffect(() => {
    void Promise.all([
      api<{ items: Listing[]; totalCount: number }>("/listings?pageSize=3"),
      api<Category[]>("/categories"),
    ]).then(([listings, categoryList]) => {
      setRecent(listings.items);
      setTotal(listings.totalCount);
      setCategories(categoryList);
    });
  }, []);
  return (
    <main>
      <section className="hero-new">
        <p className="eyebrow">MATERIAIS QUE GANHAM NOVO DESTINO</p>
        <h1>Uma obra mais econômica começa com uma escolha inteligente.</h1>
        <p>
          Compre e anuncie excedentes de construção com segurança, perto de
          você.
        </p>
        <div>
          <Link className="primary-button" to="/materiais">
            Encontrar materiais
          </Link>
          <Link className="secondary-button" to="/anunciar">
            Quero anunciar
          </Link>
        </div>
      </section>
      <section className="home-stats"><div><strong>{total}</strong><span>materiais disponíveis</span></div><div><strong>{categories.length}</strong><span>categorias para explorar</span></div><div><strong>5</strong><span>fotos por anúncio</span></div></section>
      <section className="home-content"><div className="section-title"><div><p className="eyebrow">RECÉM-PUBLICADOS</p><h2>Materiais que acabaram de chegar</h2></div><Link className="secondary-button" to="/materiais">Ver todos</Link></div><div className="card-grid">{recent.map((item) => <Card key={item.id} item={item} />)}</div></section>
      <section className="home-categories"><p className="eyebrow">EXPLORE POR CATEGORIA</p><div>{categories.map((category) => <Link key={category.id} to={`/materiais?category=${category.slug}`}>{category.name}<span>→</span></Link>)}</div></section>
      <section className="feature-row">
        <article>
          <b>01</b>
          <h3>Encontre</h3>
          <p>Filtre por material, localização e preço.</p>
        </article>
        <article>
          <b>02</b>
          <h3>Conheça</h3>
          <p>Veja fotos, detalhes e o perfil do vendedor.</p>
        </article>
        <article>
          <b>03</b>
          <h3>Construa</h3>
          <p>Combine pelo WhatsApp e reaproveite.</p>
        </article>
      </section>
    </main>
  );
}

function Catalog({ profile }: { profile: Profile | null }) {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const [items, setItems] = useState<Listing[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [favoriteIds, setFavoriteIds] = useState<Set<string>>(new Set());
  const [filtersOpen, setFiltersOpen] = useState(
    () => !window.matchMedia("(max-width: 800px)").matches,
  );
  const page = Math.max(1, Number(params.get("page") ?? 1));
  const pageSize = 12;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  useEffect(() => {
    void api<Category[]>("/categories").then(setCategories);
  }, []);
  useEffect(() => {
    if (!profile) return;
    void api<Listing[]>("/favorites").then((favorites) =>
      setFavoriteIds(new Set(favorites.map((item) => item.id))),
    );
  }, [profile]);
  useEffect(() => {
    const query = new URLSearchParams(params);
    query.set("pageSize", String(pageSize));
    void api<{ items: Listing[]; totalCount: number }>(`/listings?${query}`)
      .then((result) => {
        setItems(result.items);
        setTotal(result.totalCount);
      })
      .finally(() => setLoading(false));
  }, [params]);
  const update = (name: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(name, value);
    else next.delete(name);
    if (name !== "page") next.delete("page");
    setLoading(true);
    setParams(next);
  };
  const toggleFavorite = async (item: Listing) => {
    if (!profile) return navigate("/entrar");
    const isFavorite = favoriteIds.has(item.id);
    await api<void>(`/favorites/${item.id}`, {
      method: isFavorite ? "DELETE" : "POST",
    });
    setFavoriteIds((current) => {
      const next = new Set(current);
      if (isFavorite) next.delete(item.id);
      else next.add(item.id);
      return next;
    });
  };
  return (
    <main className="page">
      <p className="eyebrow">CATÁLOGO</p>
      <h1>Materiais disponíveis</h1>
      <div className="catalog-layout">
        <aside className="filters">
          <button
            className="filters-toggle"
            type="button"
            aria-expanded={filtersOpen}
            onClick={() => setFiltersOpen((open) => !open)}
          >
            <span>Filtros</span>
            <span aria-hidden="true">{filtersOpen ? "−" : "+"}</span>
          </button>
          <div
            className={filtersOpen ? "filters-panel open" : "filters-panel"}
            hidden={!filtersOpen}
          >
          <label>
            Buscar
            <input
              value={params.get("search") ?? ""}
              maxLength={160}
              onChange={(e) => update("search", e.target.value)}
              placeholder="Ex.: porcelanato"
            />
          </label>
          <label>
            Categoria
            <select
              value={params.get("category") ?? ""}
              onChange={(e) => update("category", e.target.value)}
            >
              <option value="">Todas</option>
              {categories.map((c) => (
                <option key={c.id} value={c.slug}>
                  {c.name}
                </option>
              ))}
            </select>
          </label>
          <div className="two">
            <label>
              Cidade
              <input
                maxLength={100}
                value={params.get("city") ?? ""}
                onChange={(e) => update("city", e.target.value)}
              />
            </label>
            <label>
              UF
              <input
                maxLength={2}
                value={params.get("state") ?? ""}
                onChange={(e) => update("state", e.target.value.toUpperCase())}
              />
            </label>
          </div>
          <div className="two">
            <label>
              Bairro
              <input
                maxLength={100}
                value={params.get("neighborhood") ?? ""}
                onChange={(e) => update("neighborhood", e.target.value)}
              />
            </label>
            <label>
              CEP
              <input
                inputMode="numeric"
                maxLength={9}
                value={formatPostalCode(params.get("postalCode") ?? "")}
                onChange={(e) => update("postalCode", formatPostalCode(e.target.value))}
              />
            </label>
          </div>
          <div className="two">
            <label>
              Preço mínimo
              <input
                type="number"
                min="0"
                value={params.get("minPrice") ?? ""}
                onChange={(e) => update("minPrice", e.target.value)}
              />
            </label>
            <label>
              Preço máximo
              <input
                type="number"
                min="0"
                value={params.get("maxPrice") ?? ""}
                onChange={(e) => update("maxPrice", e.target.value)}
              />
            </label>
          </div>
          <label>
            Condição
            <select
              value={params.get("condition") ?? ""}
              onChange={(e) => update("condition", e.target.value)}
            >
              <option value="">Todas</option>
              <option value="0">Ótimo estado</option>
              <option value="1">Bom estado</option>
              <option value="2">Estado regular</option>
            </select>
          </label>
          {(params.get("search") ||
            params.get("category") ||
            params.get("city") ||
            params.get("state") ||
            params.get("neighborhood") ||
            params.get("postalCode") ||
            params.get("minPrice") ||
            params.get("maxPrice") ||
            params.get("condition")) && (
            <button
              type="button"
              className="clear-filters"
              onClick={() => {
                setLoading(true);
                setParams(new URLSearchParams());
              }}
            >
              Limpar filtros
            </button>
          )}
          </div>
        </aside>
        <section>
          <div className="results-heading">
            <span>{total} materiais encontrados</span>
            <select
              value={params.get("sort") ?? "recent"}
              onChange={(e) => update("sort", e.target.value)}
            >
              <option value="recent">Mais recentes</option>
              <option value="priceAsc">Menor preço</option>
              <option value="priceDesc">Maior preço</option>
            </select>
          </div>
          {loading ? (
            <p>Carregando materiais...</p>
          ) : items.length ? (
            <div className="card-grid">
              {items.map((item) => (
                <Card
                  key={item.id}
                  item={item}
                  favorite={Boolean(profile) && favoriteIds.has(item.id)}
                  onFavorite={toggleFavorite}
                />
              ))}
            </div>
          ) : (
            <div className="empty">
              Nenhum material encontrado. Ajuste os filtros ou publique o
              primeiro anúncio.
            </div>
          )}
          {totalPages > 1 && <nav className="pagination" aria-label="Paginação"><button disabled={page === 1} onClick={() => update("page", String(page - 1))}>← Anterior</button><span>Página {page} de {totalPages}</span><button disabled={page === totalPages} onClick={() => update("page", String(page + 1))}>Próxima →</button></nav>}
        </section>
      </div>
    </main>
  );
}

function DetailPage({ profile }: { profile: Profile | null }) {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const [detail, setDetail] = useState<Detail | null>(null);
  const [active, setActive] = useState(0);
  const [message, setMessage] = useState("");
  const [favorite, setFavorite] = useState(false);
  useEffect(() => {
    void api<Detail>(`/listings/${id}/details`)
      .then(setDetail)
      .catch(() => navigate("/materiais"));
  }, [id, navigate]);
  useEffect(() => {
    if (profile) void api<Listing[]>("/favorites").then((items) => setFavorite(items.some((item) => item.id === id)));
  }, [profile, id]);
  if (!detail) return <main className="page">Carregando material...</main>;
  const images = detail.images.length
    ? detail.images
    : detail.listing.imageUrl
      ? [{ id: "cover", url: detail.listing.imageUrl, sortOrder: 0 }]
      : [];
  const changeImage = (direction: number) => {
    if (images.length < 2) return;
    setActive(
      (current) => (current + direction + images.length) % images.length,
    );
  };
  const interest = async () => {
    if (!profile) {
      navigate("/entrar");
      return;
    }
    try {
      await api<void>(`/listings/${id}/interests`, { method: "POST" });
      setMessage(
        "Interesse registrado. O anunciante verá seu WhatsApp no painel.",
      );
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Não foi possível registrar o interesse.",
      );
    }
  };
  const toggleFavorite = async () => {
    if (!profile) return navigate("/entrar");
    await api<void>(`/favorites/${id}`, { method: favorite ? "DELETE" : "POST" });
    setFavorite(!favorite);
  };
  return (
    <main className="page detail">
      <Link to="/materiais" className="back">
        ← Voltar aos materiais
      </Link>
      <div className="detail-grid">
        <section>
          <div className="gallery-main">
            {images[active] ? (
              <img
                src={fileUrl(images[active].url)}
                alt={`${detail.listing.title} — foto ${active + 1}`}
              />
            ) : (
              <span>Sem imagens</span>
            )}
            {images.length > 1 && (
              <>
                <button
                  className="gallery-arrow previous"
                  type="button"
                  aria-label="Foto anterior"
                  onClick={() => changeImage(-1)}
                >
                  ‹
                </button>
                <button
                  className="gallery-arrow next"
                  type="button"
                  aria-label="Próxima foto"
                  onClick={() => changeImage(1)}
                >
                  ›
                </button>
                <span className="gallery-counter">
                  {active + 1} / {images.length}
                </span>
              </>
            )}
          </div>
          {images.length > 1 && (
            <div className="thumbnails">
              {images.map((image, index) => (
                <button
                  key={image.id}
                  className={active === index ? "selected" : ""}
                  onClick={() => setActive(index)}
                  style={{ backgroundImage: `url(${fileUrl(image.url)})` }}
                />
              ))}
            </div>
          )}
        </section>
        <section className="detail-info">
          <p className="eyebrow">{detail.listing.category}</p>
          <h1>{detail.listing.title}</h1>
          <p className="detail-price">
            {money(detail.listing.price)}{" "}
            <small>por {detail.listing.unit}</small>
          </p>
          <p>{detail.listing.description}</p>
          <dl>
            <div>
              <dt>Quantidade</dt>
              <dd>
                {detail.listing.quantity} {detail.listing.unit}
              </dd>
            </div>
            <div>
              <dt>Condição</dt>
              <dd>{condition(detail.listing.condition)}</dd>
            </div>
            <div>
              <dt>Localização</dt>
              <dd>
                {detail.listing.neighborhood
                  ? `${detail.listing.neighborhood}, `
                  : ""}
                {detail.listing.city}, {detail.listing.state}
              </dd>
            </div>
          </dl>
          {message && <p className="notice">{message}</p>}
          <div className="detail-actions"><button className="primary-button" onClick={() => void interest()}>Tenho interesse</button><button className={favorite ? "secondary-button favorite-action selected" : "secondary-button favorite-action"} onClick={() => void toggleFavorite()}>{favorite ? "♥ Favoritado" : "♡ Favoritar"}</button></div>
          <aside className="seller-card">
            <small>ANUNCIADO POR</small>
            <h3>
              {detail.seller?.displayName ?? detail.listing.sellerDisplayName}
            </h3>
            <p>
              {detail.seller?.city ?? detail.listing.city},{" "}
              {detail.seller?.state ?? detail.listing.state}
            </p>
            {detail.seller && (
              <p>
                Membro desde{" "}
                {new Date(detail.seller.createdAtUtc).getFullYear()}
              </p>
            )}
          </aside>
        </section>
      </div>
    </main>
  );
}

function Auth({ setProfile }: { setProfile: (p: Profile) => void }) {
  const navigate = useNavigate();
  const [register, setRegister] = useState(false);
  const [form, setForm] = useState({
    displayName: "",
    email: "",
    password: "",
    city: "",
    state: "",
    whatsApp: "",
  });
  const [error, setError] = useState("");
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      const profile = await api<Profile>(
        `/auth/${register ? "register" : "login"}`,
        {
          method: "POST",
          body: JSON.stringify(
            register ? form : { email: form.email, password: form.password },
          ),
        },
      );
      setProfile(profile);
      navigate("/perfil");
    } catch (x) {
      setError(x instanceof Error ? x.message : "Não foi possível entrar.");
    }
  };
  return (
    <main className="auth page">
      <p className="eyebrow">SUA CONTA</p>
      <h1>{register ? "Crie sua conta" : "Entre na Bricker"}</h1>
      <form onSubmit={submit}>
        {register && (
          <label>
            Nome
            <input
              required
              minLength={2}
              maxLength={100}
              onChange={(e) =>
                setForm({ ...form, displayName: e.target.value })
              }
            />
          </label>
        )}
        <label>
          E-mail
          <input
            required
            type="email"
            maxLength={254}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
          />
        </label>
        <label>
          Senha
          <input
            required
            type="password"
            minLength={8}
            maxLength={128}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
          />
        </label>
        {register && (
          <>
            <label>
              WhatsApp
              <input
                required
                inputMode="tel"
                maxLength={15}
                placeholder="(47) 99999-9999"
                value={form.whatsApp}
                onChange={(e) =>
                  setForm({ ...form, whatsApp: formatPhone(e.target.value) })
                }
                pattern="\([0-9]{2}\) [0-9]{4,5}-[0-9]{4}"
                title="Informe o DDD e um telefone com 10 ou 11 números"
              />
            </label>
            <div className="two">
              <label>
                Cidade
                <input
                  maxLength={100}
                  onChange={(e) => setForm({ ...form, city: e.target.value })}
                />
              </label>
              <label>
                UF
                <input
                  maxLength={2}
                  pattern="[A-Za-z]{2}"
                  onChange={(e) =>
                    setForm({ ...form, state: e.target.value.toUpperCase() })
                  }
                />
              </label>
            </div>
          </>
        )}{" "}
        {error && <p className="form-error">{error}</p>}
        <button className="primary-button full">
          {register ? "Criar conta" : "Entrar"}
        </button>
      </form>
      <button className="link-button" onClick={() => setRegister(!register)}>
        {register ? "Já tenho uma conta" : "Ainda não tenho conta"}
      </button>
    </main>
  );
}

function LocalImagePreview({
  file,
  onRemove,
  index,
  total,
  onMove,
  onCover,
  isCover,
}: {
  file: File;
  onRemove: () => void;
  index: number;
  total: number;
  onMove: (direction: number) => void;
  onCover: () => void;
  isCover: boolean;
}) {
  const [source] = useState(() => URL.createObjectURL(file));
  useEffect(() => () => URL.revokeObjectURL(source), [source]);
  return (
    <div className="upload-preview">
      <img src={source} alt={file.name} />
      <button
        type="button"
        onClick={onRemove}
        aria-label={`Remover ${file.name}`}
      >
        ×
      </button>
      <span>{file.name}</span>
      <div className="preview-actions">
        <button type="button" disabled={index === 0} onClick={() => onMove(-1)}>
          ←
        </button>
        <button type="button" disabled={index === total - 1} onClick={() => onMove(1)}>
          →
        </button>
        <button type="button" disabled={isCover} onClick={onCover}>
          {isCover ? "Capa atual" : "Capa"}
        </button>
      </div>
    </div>
  );
}

function Announce({ profile }: { profile: Profile | null }) {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);
  const [categories, setCategories] = useState<Category[]>([]);
  const [files, setFiles] = useState<File[]>([]);
  const [coverFile, setCoverFile] = useState<File | null>(null);
  const [existingImages, setExistingImages] = useState<Detail["images"]>([]);
  const [loading, setLoading] = useState(Boolean(id));
  const [postalLoading, setPostalLoading] = useState(false);
  const [step, setStep] = useState(1);
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    categoryId: "",
    title: "",
    description: "",
    price: "",
    quantity: "",
    unit: "unidade",
    condition: "0",
    city: profile?.city ?? "",
    state: profile?.state ?? "",
    postalCode: "",
    street: "",
    neighborhood: "",
    addressNumber: "",
    addressComplement: "",
  });
  useEffect(() => {
    void api<Category[]>("/categories").then((items) => {
      setCategories(items);
      setForm((value) => ({ ...value, categoryId: items[0]?.id ?? "" }));
    });
  }, []);
  useEffect(() => {
    if (!id) return;
    void api<Detail>(`/listings/${id}/details`)
      .then((detail) => {
        const item = detail.listing;
        setForm({
          categoryId:
            categories.find((category) => category.slug === item.categorySlug)
              ?.id ?? "",
          title: item.title,
          description: item.description,
          price: String(item.price),
          quantity: String(item.quantity),
          unit: item.unit,
          condition: String(item.condition),
          city: item.city,
          state: item.state,
          postalCode: formatPostalCode(item.postalCode ?? ""),
          street: item.street ?? "",
          neighborhood: item.neighborhood ?? "",
          addressNumber: item.addressNumber ?? "",
          addressComplement: item.addressComplement ?? "",
        });
        setExistingImages(detail.images);
      })
      .catch(() =>
        setError("Não foi possível carregar este material para edição."),
      )
      .finally(() => setLoading(false));
  }, [id, categories]);
  if (!profile) return <Navigate to="/entrar" />;
  const validateDetails = () => {
    if (form.title.trim().length < 5 || form.title.trim().length > 160)
      return "O título deve ter entre 5 e 160 caracteres.";
    if (
      form.description.trim().length < 20 ||
      form.description.trim().length > 2000
    )
      return "A descrição deve ter entre 20 e 2.000 caracteres.";
    if (!form.categoryId) return "Selecione uma categoria.";
    if (!form.price || Number(form.price) <= 0) return "Informe um preço válido.";
    if (!form.quantity || Number(form.quantity) <= 0)
      return "Informe uma quantidade válida.";
    if (!form.unit.trim() || form.unit.trim().length > 24)
      return "A unidade deve ter entre 1 e 24 caracteres.";
    return "";
  };
  const validateAddress = () => {
    if (onlyDigits(form.postalCode).length !== 8)
      return "Informe um CEP válido com 8 números.";
    if (form.street.trim().length < 2) return "Informe o logradouro.";
    if (form.neighborhood.trim().length < 2) return "Informe o bairro.";
    if (form.city.trim().length < 2) return "Informe a cidade.";
    if (!/^[A-Za-z]{2}$/.test(form.state)) return "Informe uma UF válida.";
    if (!form.addressNumber.trim()) return "Informe o número do endereço.";
    return "";
  };
  const nextStep = () => {
    const validation = step === 1 ? validateDetails() : validateAddress();
    if (validation) return setError(validation);
    setError("");
    setStep((current) => Math.min(3, current + 1));
  };
  const lookupPostalCode = async () => {
    const postalCode = onlyDigits(form.postalCode);
    if (postalCode.length !== 8) {
      setError("Informe um CEP válido com 8 números.");
      return;
    }
    setPostalLoading(true);
    setError("");
    try {
      const response = await fetch(`https://viacep.com.br/ws/${postalCode}/json/`);
      if (!response.ok) throw new Error();
      const address = (await response.json()) as {
        erro?: boolean;
        logradouro?: string;
        bairro?: string;
        localidade?: string;
        uf?: string;
      };
      if (address.erro) throw new Error();
      setForm((current) => ({
        ...current,
        postalCode: formatPostalCode(postalCode),
        street: address.logradouro ?? current.street,
        neighborhood: address.bairro ?? current.neighborhood,
        city: address.localidade ?? current.city,
        state: address.uf ?? current.state,
      }));
    } catch {
      setError("CEP não encontrado. Confira os números ou preencha o endereço manualmente.");
    } finally {
      setPostalLoading(false);
    }
  };
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const validation = validateDetails() || validateAddress();
    if (validation) return setError(validation);
    if (files.length + existingImages.length === 0)
      return setError("Adicione pelo menos uma foto ao anúncio.");
    if (files.length + existingImages.length > 5)
      return setError("Você pode enviar no máximo 5 fotos.");
    const data = new FormData();
    Object.entries(form).forEach(([key, value]) => data.append(key, value));
    files.forEach((file) => data.append("images", file));
    if (coverFile) data.append("newCoverIndex", String(files.indexOf(coverFile)));
    try {
      const listing = await api<Listing>(
        isEditing ? `/listings/${id}` : "/listings",
        {
          method: isEditing ? "PUT" : "POST",
          body: data,
        },
      );
      navigate(`/materiais/${listing.id}`);
    } catch (x) {
      setError(x instanceof Error ? x.message : "Não foi possível publicar.");
    }
  };
  const removeExistingImage = async (imageId: string) => {
    if (!id || imageId === "00000000-0000-0000-0000-000000000000") return;
    if (!window.confirm("Remover esta foto do anúncio?")) return;
    try {
      await api<void>(`/listings/${id}/images/${imageId}`, {
        method: "DELETE",
      });
      setExistingImages((current) =>
        current.filter((image) => image.id !== imageId),
      );
    } catch (x) {
      setError(
        x instanceof Error ? x.message : "Não foi possível remover a foto.",
      );
    }
  };
  const reorderExisting = async (from: number, to: number) => {
    if (!id || to < 0 || to >= existingImages.length || from === to) return;
    const reordered = [...existingImages];
    const [moved] = reordered.splice(from, 1);
    reordered.splice(to, 0, moved);
    try {
      await api<void>(`/listings/${id}/images/order`, {
        method: "PUT",
        body: JSON.stringify({ imageIds: reordered.map((image) => image.id) }),
      });
      setExistingImages(
        reordered.map((image, index) => ({ ...image, sortOrder: index })),
      );
      setCoverFile(null);
    } catch (x) {
      setError(
        x instanceof Error ? x.message : "Não foi possível ordenar as fotos.",
      );
    }
  };
  const reorderFile = (from: number, to: number) => {
    if (to < 0 || to >= files.length || from === to) return;
    setFiles((current) => {
      const reordered = [...current];
      const [moved] = reordered.splice(from, 1);
      reordered.splice(to, 0, moved);
      return reordered;
    });
  };
  const selectFiles = (selected: File[]) => {
    const allowedTypes = ["image/jpeg", "image/png", "image/webp"];
    if (selected.some((file) => !allowedTypes.includes(file.type))) {
      setError("Envie somente fotos JPG, PNG ou WEBP.");
      return;
    }
    if (selected.some((file) => file.size > 5 * 1024 * 1024)) {
      setError("Cada foto deve ter no máximo 5 MB.");
      return;
    }
    const available = 5 - existingImages.length - files.length;
    if (selected.length > available) {
      setError(`Você ainda pode adicionar ${Math.max(available, 0)} foto(s).`);
      return;
    }
    setError("");
    setFiles((current) => [...current, ...selected]);
  };
  if (loading)
    return <main className="page form-page">Carregando material...</main>;
  return (
    <main className="page form-page">
      <p className="eyebrow">ANUNCIAR MATERIAL</p>
      <h1>{isEditing ? "Edite seu material" : "Publique seu excedente"}</h1>
      <p>
        Fotos claras e informações completas ajudam seu material a encontrar uma
        nova obra.
      </p>
      <form onSubmit={submit}>
        <ol className="form-steps" aria-label="Etapas do anúncio">
          {["Material", "Localização", "Fotos"].map((label, index) => (
            <li className={step >= index + 1 ? "active" : ""} key={label}>
              <span>{index + 1}</span>{label}
            </li>
          ))}
        </ol>
        {step === 1 && (
          <section className="form-step">
            <div className="step-heading"><span>ETAPA 1 DE 3</span><h2>Dados do material</h2></div>
            <label>Categoria<select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>{categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label>
            <label>Título<input minLength={5} maxLength={160} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /><small>{form.title.length}/160 caracteres</small></label>
            <label>Descrição<textarea minLength={20} maxLength={2000} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /><small>{form.description.length}/2.000 caracteres</small></label>
            <div className="two">
              <label>Preço (R$)<input type="number" min=".01" max="9999999999.99" step=".01" value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} /></label>
              <label>Quantidade<input type="number" min=".01" max="9999999999.99" step=".01" value={form.quantity} onChange={(e) => setForm({ ...form, quantity: e.target.value })} /></label>
            </div>
            <div className="two">
              <label>Unidade<input maxLength={24} value={form.unit} onChange={(e) => setForm({ ...form, unit: e.target.value })} /></label>
              <label>Condição<select value={form.condition} onChange={(e) => setForm({ ...form, condition: e.target.value })}><option value="0">Ótimo estado</option><option value="1">Bom estado</option><option value="2">Estado regular</option></select></label>
            </div>
          </section>
        )}
        {step === 2 && (
          <section className="form-step">
            <div className="step-heading"><span>ETAPA 2 DE 3</span><h2>Localização para retirada</h2><p>O endereço ajuda a localizar o material. Publicamente mostramos apenas bairro, cidade e UF.</p></div>
            <div className="postal-row">
              <label>CEP<input inputMode="numeric" maxLength={9} placeholder="00000-000" value={form.postalCode} onChange={(e) => setForm({ ...form, postalCode: formatPostalCode(e.target.value) })} onBlur={() => { if (onlyDigits(form.postalCode).length === 8 && !form.street) void lookupPostalCode(); }} /></label>
              <button className="secondary-button" type="button" disabled={postalLoading} onClick={() => void lookupPostalCode()}>{postalLoading ? "Buscando..." : "Buscar CEP"}</button>
            </div>
            <label>Logradouro<input maxLength={150} value={form.street} onChange={(e) => setForm({ ...form, street: e.target.value })} /></label>
            <div className="two">
              <label>Número<input maxLength={20} value={form.addressNumber} onChange={(e) => setForm({ ...form, addressNumber: e.target.value })} /></label>
              <label>Complemento (opcional)<input maxLength={100} value={form.addressComplement} onChange={(e) => setForm({ ...form, addressComplement: e.target.value })} /></label>
            </div>
            <label>Bairro<input maxLength={100} value={form.neighborhood} onChange={(e) => setForm({ ...form, neighborhood: e.target.value })} /></label>
            <div className="two">
              <label>Cidade<input maxLength={100} value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} /></label>
              <label>UF<input maxLength={2} value={form.state} onChange={(e) => setForm({ ...form, state: e.target.value.replace(/[^A-Za-z]/g, "").toUpperCase() })} /></label>
            </div>
          </section>
        )}
        {step === 3 && (
          <section className="form-step">
            <div className="step-heading"><span>ETAPA 3 DE 3</span><h2>Fotos e publicação</h2><p>Adicione até cinco fotos e escolha qual será a capa do anúncio.</p></div>
            <label className="upload-zone">Adicionar fotos (até 5)<input type="file" accept="image/jpeg,image/png,image/webp" multiple onChange={(e) => { selectFiles(Array.from(e.target.files ?? [])); e.target.value = ""; }} /><span>{files.length ? `${files.length} foto(s) nova(s) selecionada(s)` : "JPG, PNG ou WEBP · até 5 MB por foto"}</span></label>
            {(existingImages.length > 0 || files.length > 0) && (
              <div className="upload-previews">
                {existingImages.map((image, index) => (
                  <div className="upload-preview" key={image.id}>
                    <img src={fileUrl(image.url)} alt={`Foto atual ${index + 1}`} />
                    <button type="button" onClick={() => void removeExistingImage(image.id)} aria-label={`Remover foto ${index + 1}`}>×</button>
                    <span>{!coverFile && index === 0 ? "Foto de capa" : `Foto ${index + 1}`}</span>
                    <div className="preview-actions">
                      <button type="button" disabled={index === 0} onClick={() => void reorderExisting(index, index - 1)}>←</button>
                      <button type="button" disabled={index === existingImages.length - 1} onClick={() => void reorderExisting(index, index + 1)}>→</button>
                      {index > 0 && <button type="button" onClick={() => void reorderExisting(index, 0)}>Capa</button>}
                    </div>
                  </div>
                ))}
                {files.map((file, index) => (
                  <LocalImagePreview key={`${file.name}-${file.lastModified}`} file={file} index={index} total={files.length} onMove={(direction) => reorderFile(index, index + direction)} onCover={() => setCoverFile(file)} isCover={coverFile === file || (!isEditing && !coverFile && index === 0)} onRemove={() => setFiles((current) => { if (coverFile === file) setCoverFile(null); return current.filter((_, itemIndex) => itemIndex !== index); })} />
                ))}
              </div>
            )}
            <aside className="publish-summary"><strong>{form.title || "Seu material"}</strong><span>{money(Number(form.price || 0))} · {form.quantity || 0} {form.unit}</span><span>{form.neighborhood ? `${form.neighborhood}, ` : ""}{form.city}, {form.state}</span></aside>
          </section>
        )}
        {error && <p className="form-error">{error}</p>}
        <div className="step-actions">
          {step > 1 && <button className="secondary-button" type="button" onClick={() => { setError(""); setStep((current) => current - 1); }}>← Voltar</button>}
          {step < 3 ? <button className="primary-button" type="button" onClick={nextStep}>Continuar →</button> : <button className="primary-button" type="submit">{isEditing ? "Salvar alterações" : "Publicar material"}</button>}
        </div>
      </form>
    </main>
  );
}

function ProfilePage({
  profile,
  setProfile,
}: {
  profile: Profile | null;
  setProfile: (p: Profile) => void;
}) {
  const [mine, setMine] = useState<Listing[]>([]);
  const [interests, setInterests] = useState<Interest[]>([]);
  const [favorites, setFavorites] = useState<Listing[]>([]);
  const [sentInterests, setSentInterests] = useState<SentInterest[]>([]);
  const [editing, setEditing] = useState(false);
  const [profileError, setProfileError] = useState("");
  const [form, setForm] = useState(profile);
  useEffect(() => {
    if (profile) {
      void api<Listing[]>("/listings/mine").then(setMine);
      void api<Interest[]>("/listings/mine/interests").then(setInterests);
      void api<Listing[]>("/favorites").then(setFavorites);
      void api<SentInterest[]>("/listings/interests/sent").then(
        setSentInterests,
      );
    }
  }, [profile]);
  if (!profile || !form) return <Navigate to="/entrar" />;
  const save = async (e: FormEvent) => {
    e.preventDefault();
    setProfileError("");
    try {
      const updated = await api<Profile>("/profile", {
        method: "PUT",
        body: JSON.stringify({
          displayName: form.displayName,
          city: form.city,
          state: form.state,
          whatsApp: form.whatsApp,
        }),
      });
      setProfile(updated);
      setEditing(false);
    } catch (error) {
      setProfileError(
        error instanceof Error ? error.message : "Não foi possível salvar o perfil.",
      );
    }
  };
  const updateStatus = async (listingId: string, status: number) => {
    await api<void>(`/listings/${listingId}/status`, {
      method: "PUT",
      body: JSON.stringify({ status }),
    });
    setMine((current) =>
      current.map((item) =>
        item.id === listingId ? { ...item, status } : item,
      ),
    );
  };
  const removeFavorite = async (item: Listing) => {
    await api<void>(`/favorites/${item.id}`, { method: "DELETE" });
    setFavorites((current) => current.filter((favorite) => favorite.id !== item.id));
  };
  return (
    <main className="page profile-page">
      <section className="profile-hero">
        <div className="profile-avatar">
          {profile.displayName.charAt(0).toUpperCase()}
        </div>
        <div className="profile-identity">
          <p className="eyebrow">MEU PERFIL</p>
          <h1>{profile.displayName}</h1>
          <p>
            {profile.email} · {profile.city || "Cidade não informada"}
            {profile.state ? `, ${profile.state}` : ""}
          </p>
        </div>
        <button
          className="secondary-button"
          onClick={() => {
            if (!editing)
              setForm({
                ...profile,
                whatsApp: formatPhone(profile.whatsApp ?? ""),
              });
            setEditing(!editing);
          }}
        >
          {editing ? "Cancelar edição" : "Editar perfil"}
        </button>
      </section>
      {editing && (
        <form className="profile-form profile-edit-card" onSubmit={save}>
          <div className="form-heading">
            <div>
              <span>INFORMAÇÕES PESSOAIS</span>
              <h2>Atualize seu perfil</h2>
            </div>
            <p>
              Seu WhatsApp só aparece para anunciantes quando você demonstra
              interesse.
            </p>
          </div>
          <label>
            Nome
            <input
              required
              minLength={2}
              maxLength={100}
              value={form.displayName}
              onChange={(e) =>
                setForm({ ...form, displayName: e.target.value })
              }
            />
          </label>
          <label>
            WhatsApp
            <input
              inputMode="tel"
              maxLength={15}
              pattern="\([0-9]{2}\) [0-9]{4,5}-[0-9]{4}"
              title="Informe o DDD e um telefone com 10 ou 11 números"
              value={formatPhone(form.whatsApp ?? "")}
              onChange={(e) =>
                setForm({ ...form, whatsApp: formatPhone(e.target.value) })
              }
            />
          </label>
          <div className="two">
            <label>
              Cidade
              <input
                maxLength={100}
                value={form.city ?? ""}
                onChange={(e) => setForm({ ...form, city: e.target.value })}
              />
            </label>
            <label>
              UF
              <input
                maxLength={2}
                pattern="[A-Za-z]{2}"
                value={form.state ?? ""}
                onChange={(e) =>
                  setForm({ ...form, state: e.target.value.toUpperCase() })
                }
              />
            </label>
          </div>
          {profileError && <p className="form-error">{profileError}</p>}
          <button className="primary-button">Salvar perfil</button>
        </form>
      )}
      <section className="profile-section">
        <div className="section-title profile-section-heading">
          <div>
            <span className="section-kicker">SEUS ANÚNCIOS</span>
            <h2>Meus materiais</h2>
          </div>
          <Link className="primary-button" to="/anunciar">
            + Novo anúncio
          </Link>
        </div>
        <div className="my-list">
          {mine.length ? (
            mine.map((item) => (
              <div className="owned-card" key={item.id}>
                <Card item={item} />
                <div className="owned-actions">
                  <label>
                    Status
                    <select
                      value={item.status}
                      onChange={(event) =>
                        void updateStatus(item.id, Number(event.target.value))
                      }
                    >
                      <option value="1">Disponível</option>
                      <option value="2">Reservado</option>
                      <option value="3">Vendido</option>
                      <option value="4">Inativo</option>
                    </select>
                  </label>
                  {item.status === 1 && (
                    <Link
                      className="edit-listing-button"
                      to={`/anunciar/${item.id}`}
                    >
                      Editar material
                    </Link>
                  )}
                </div>
              </div>
            ))
          ) : (
            <p className="empty">Você ainda não publicou materiais.</p>
          )}
        </div>
      </section>
      <section className="profile-section">
        <div className="profile-section-heading">
          <span className="section-kicker">SALVOS</span>
          <h2>Meus favoritos</h2>
        </div>
        {favorites.length ? (
          <div className="my-list">
            {favorites.map((item) => (
              <Card
                key={item.id}
                item={item}
                favorite
                onFavorite={removeFavorite}
              />
            ))}
          </div>
        ) : (
          <p className="empty">Os materiais que você favoritar aparecerão aqui.</p>
        )}
      </section>
      <section className="profile-section">
        <div className="profile-section-heading">
          <span className="section-kicker">CONTATOS</span>
          <h2>Interesses recebidos</h2>
        </div>
        {interests.length ? (
          <div className="interest-list">
            {interests.map((item) => (
              <article key={item.id}>
                <strong>{item.listingTitle}</strong>
                <span>
                  {item.displayName} · {item.email}
                </span>
                {item.whatsApp && (
                  <a
                    href={`https://wa.me/${item.whatsApp.replace(/\D/g, "")}`}
                    target="_blank"
                  >
                    Conversar no WhatsApp
                  </a>
                )}
              </article>
            ))}
          </div>
        ) : (
          <p className="empty">
            Quando alguém se interessar pelos seus materiais, o contato
            aparecerá aqui.
          </p>
        )}
      </section>
      <section className="profile-section">
        <div className="profile-section-heading">
          <span className="section-kicker">ACOMPANHAMENTO</span>
          <h2>Interesses enviados</h2>
        </div>
        {sentInterests.length ? (
          <div className="interest-list">
            {sentInterests.map((item) => (
              <article key={item.id}>
                <div>
                  <strong>{item.listingTitle}</strong>
                  <span>Anunciado por {item.sellerDisplayName}</span>
                </div>
                <span className={`status-badge status-${item.listingStatus}`}>
                  {listingStatus(item.listingStatus)}
                </span>
                {item.listingStatus === 1 && (
                  <Link to={`/materiais/${item.listingId}`}>Ver material</Link>
                )}
              </article>
            ))}
          </div>
        ) : (
          <p className="empty">Seus interesses enviados aparecerão aqui.</p>
        )}
      </section>
    </main>
  );
}

function App() {
  const [profile, setProfile] = useState<Profile | null>(null);
  useEffect(() => {
    void api<Profile>("/profile")
      .then(setProfile)
      .catch(() => {});
  }, []);
  return (
    <Layout profile={profile} setProfile={setProfile}>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/materiais" element={<Catalog profile={profile} />} />
        <Route
          path="/materiais/:id"
          element={<DetailPage profile={profile} />}
        />
        <Route path="/anunciar" element={<Announce profile={profile} />} />
        <Route path="/anunciar/:id" element={<Announce profile={profile} />} />
        <Route
          path="/perfil"
          element={<ProfilePage profile={profile} setProfile={setProfile} />}
        />
        <Route path="/entrar" element={<Auth setProfile={setProfile} />} />
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </Layout>
  );
}
export default App;
