import { useCallback, useEffect, useState, type FormEvent } from "react";
import "./App.css";

type Category = { id: string; name: string; slug: string };
type Listing = {
  id: string;
  title: string;
  description: string;
  price: number;
  unit: string;
  quantity: number;
  city: string;
  state: string;
  category: string;
  categorySlug: string;
  condition: number;
  status: number;
  imageUrl?: string;
};
type ListingResult = { items: Listing[]; totalCount: number };
type Profile = {
  id: string;
  displayName: string;
  email: string;
  city?: string;
  state?: string;
};
type ApiError = { message?: string; errors?: Record<string, string[]> };
const apiUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5190/api/v1";
const emptyAd = {
  categoryId: "",
  title: "",
  description: "",
  price: "",
  unit: "unidade",
  quantity: "",
  condition: "0",
  city: "",
  state: "",
};

async function api<T>(path: string, options: RequestInit = {}) {
  const response = await fetch(`${apiUrl}${path}`, {
    credentials: "include",
    ...options,
    headers: options.body instanceof FormData ? options.headers : { "Content-Type": "application/json", ...options.headers },
  });
  if (!response.ok) {
    const body: ApiError = await response.json().catch(() => ({}));
    throw new Error(
      body.message ||
        (body.errors && Object.values(body.errors).flat().join(" ")) ||
        "Não foi possível concluir a operação.",
    );
  }
  return response.status === 204
    ? (undefined as T)
    : (response.json() as Promise<T>);
}

function App() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [listings, setListings] = useState<Listing[]>([]);
  const [mine, setMine] = useState<Listing[]>([]);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [search, setSearch] = useState("");
  const [city, setCity] = useState("");
  const [category, setCategory] = useState("");
  const [modal, setModal] = useState<"login" | "register" | "ad" | null>(null);
  const [editing, setEditing] = useState<Listing | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [auth, setAuth] = useState({
    displayName: "",
    email: "",
    password: "",
    city: "",
    state: "",
  });
  const [ad, setAd] = useState(emptyAd);
  const [image, setImage] = useState<File | null>(null);
  const loadListings = useCallback(
    async (filters = { search: "", city: "", category: "" }) => {
      setLoading(true);
      try {
        const q = new URLSearchParams({ pageSize: "12" });
        if (filters.search) q.set("search", filters.search);
        if (filters.city) q.set("city", filters.city);
        if (filters.category) q.set("category", filters.category);
        setListings((await api<ListingResult>(`/listings?${q}`)).items);
      } catch {
        setError(
          "Não foi possível conectar à API. Inicie o backend pelo Visual Studio.",
        );
      } finally {
        setLoading(false);
      }
    },
    [],
  );
  const loadMine = useCallback(async () => {
    if (profile) setMine(await api<Listing[]>("/listings/mine"));
  }, [profile]);
  useEffect(() => {
    void (async () => {
      try {
        setCategories(await api<Category[]>("/categories"));
        try {
          setProfile(await api<Profile>("/profile"));
        } catch {
          /* visitante */
        }
      } catch {
        setError(
          "Não foi possível conectar à API. Inicie o backend pelo Visual Studio.",
        );
      }
      await loadListings();
    })();
  }, [loadListings]);
  useEffect(() => {
    void loadMine().catch(() => setProfile(null));
  }, [loadMine]);
  const searchListings = (event: FormEvent) => {
    event.preventDefault();
    void loadListings({ search, city, category });
  };
  const openAd = (item?: Listing) => {
    if (!profile) {
      setModal("login");
      return;
    }
    setEditing(item ?? null);
    setImage(null);
    setAd(
      item
        ? {
            categoryId:
              categories.find((c) => c.slug === item.categorySlug)?.id ?? "",
            title: item.title,
            description: item.description,
            price: String(item.price),
            unit: item.unit,
            quantity: String(item.quantity),
            condition: String(item.condition),
            city: item.city,
            state: item.state,
          }
        : {
            ...emptyAd,
            categoryId: categories[0]?.id ?? "",
            city: profile.city ?? "",
            state: profile.state ?? "",
          },
    );
    setModal("ad");
  };
  const submitAuth = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    try {
      const register = modal === "register";
      const user = await api<Profile>(
        `/auth/${register ? "register" : "login"}`,
        {
          method: "POST",
          body: JSON.stringify(
            register ? auth : { email: auth.email, password: auth.password },
          ),
        },
      );
      setProfile(user);
      setMessage(`Bem-vindo, ${user.displayName}!`);
      setModal(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Falha ao entrar.");
    } finally {
      setSaving(false);
    }
  };
  const submitAd = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    try {
      const data = new FormData();
      Object.entries(ad).forEach(([key, value]) => data.append(key, value));
      if (image) data.append("image", image);
      await api(editing ? `/listings/${editing.id}` : "/listings", {
        method: editing ? "PUT" : "POST",
        body: data,
      });
      setModal(null);
      setMessage(editing ? "Anúncio atualizado." : "Anúncio publicado.");
      await Promise.all([loadMine(), loadListings({ search, city, category })]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Falha ao salvar.");
    } finally {
      setSaving(false);
    }
  };
  const deactivate = async (item: Listing) => {
    if (!confirm(`Desativar “${item.title}”?`)) return;
    try {
      await api<void>(`/listings/${item.id}`, { method: "DELETE" });
      setMessage("Anúncio desativado.");
      await Promise.all([loadMine(), loadListings({ search, city, category })]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Falha ao desativar.");
    }
  };
  const logout = async () => {
    await api<void>("/auth/logout", { method: "POST" });
    setProfile(null);
    setMine([]);
    setMessage("Você saiu da conta.");
  };
  const close = () => {
    setModal(null);
    setError("");
  };
  return (
    <main>
      <header className="topbar">
        <a className="brand" href="#inicio">
          bricker<span>.</span>
        </a>
        <nav>
          <a href="#anuncios">Explorar materiais</a>
          {profile && <a href="#meus-anuncios">Meus anúncios</a>}
          <a href="#como-funciona">Como funciona</a>
        </nav>
        <div className="header-actions">
          {profile ? (
            <>
              <span>Olá, {profile.displayName.split(" ")[0]}</span>
              <button className="text-button" onClick={() => void logout()}>
                Sair
              </button>
            </>
          ) : (
            <button className="text-button" onClick={() => setModal("login")}>
              Entrar
            </button>
          )}
          <button className="primary-button" onClick={() => openAd()}>
            Anunciar material
          </button>
        </div>
      </header>
      <section className="hero" id="inicio">
        <div>
          <p className="eyebrow">CONSTRUÇÃO CIRCULAR</p>
          <h1>O que sobra em uma obra pode construir outra.</h1>
          <p className="hero-description">
            Encontre materiais de construção excedentes perto de você. Economize
            na obra e reduza o desperdício.
          </p>
          <form className="search-box" onSubmit={searchListings}>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="O que você está procurando?"
            />
            <input
              value={city}
              onChange={(e) => setCity(e.target.value)}
              placeholder="Cidade"
            />
            <button className="primary-button">Buscar</button>
          </form>
        </div>
        <aside className="impact-card">
          <strong>
            Menos descarte.
            <br />
            Mais construção.
          </strong>
          <p>Materiais em bom estado merecem uma nova história.</p>
        </aside>
      </section>
      {message && (
        <p className="notice">
          {message}
          <button onClick={() => setMessage("")}>×</button>
        </p>
      )}
      {error && !modal && <p className="error-message">{error}</p>}
      {profile && (
        <section className="my-listings" id="meus-anuncios">
          <div className="section-heading">
            <div>
              <p className="eyebrow">ÁREA DO ANUNCIANTE</p>
              <h2>Meus anúncios</h2>
            </div>
            <button className="primary-button" onClick={() => openAd()}>
              + Novo anúncio
            </button>
          </div>
          {mine.length === 0 ? (
            <p className="catalog-message">
              Você ainda não publicou nenhum material.
            </p>
          ) : (
            <div className="my-list">
              {mine.map((item) => (
                <article className="my-item" key={item.id}>
                  <div>
                    <strong>{item.title}</strong>
                    <small>
                      {item.city}, {item.state} ·{" "}
                      {item.status === 1 ? "Ativo" : "Inativo"}
                    </small>
                  </div>
                  <b>
                    {item.price.toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}
                  </b>
                  <div>
                    <button
                      className="small-button"
                      onClick={() => openAd(item)}
                    >
                      Editar
                    </button>
                    {item.status === 1 && (
                      <button
                        className="small-button danger"
                        onClick={() => void deactivate(item)}
                      >
                        Desativar
                      </button>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      )}
      <section className="catalog" id="anuncios">
        <div className="section-heading">
          <div>
            <p className="eyebrow">MATERIAIS DISPONÍVEIS</p>
            <h2>Encontre o que a sua obra precisa</h2>
          </div>
          <span>{listings.length} materiais encontrados</span>
        </div>
        <div className="category-row">
          <button
            className={!category ? "category active" : "category"}
            onClick={() => {
              setCategory("");
              void loadListings({ search, city, category: "" });
            }}
          >
            Todos
          </button>
          {categories.map((c) => (
            <button
              key={c.id}
              className={category === c.slug ? "category active" : "category"}
              onClick={() => {
                setCategory(c.slug);
                void loadListings({ search, city, category: c.slug });
              }}
            >
              {c.name}
            </button>
          ))}
        </div>
        {loading ? (
          <p className="catalog-message">Carregando materiais...</p>
        ) : (
          <div className="listing-grid">
            {listings.map((item, index) => (
              <article className="listing-card" key={item.id}>
                <div className={`listing-image tone-${index % 3}`} style={item.imageUrl ? { backgroundImage: `url(http://localhost:5190${item.imageUrl})`, backgroundSize: "cover", backgroundPosition: "center" } : undefined}>
                  <span>{item.category}</span>
                </div>
                <div className="listing-content">
                  <p className="condition">
                    {item.condition === 0 ? "EM ÓTIMO ESTADO" : "EM BOM ESTADO"}
                  </p>
                  <h3>{item.title}</h3>
                  <p className="price">
                    {item.price.toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}{" "}
                    <small>por {item.unit}</small>
                  </p>
                  <div className="listing-meta">
                    <span>
                      {item.quantity} {item.unit}
                    </span>
                    <span>
                      {item.city}, {item.state}
                    </span>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
      <section className="steps" id="como-funciona">
        <div>
          <p className="eyebrow">SIMPLES E DIRETO</p>
          <h2>Reaproveitar começa aqui.</h2>
        </div>
        <ol>
          <li>
            <b>01</b>
            <strong>Encontre</strong>
            <p>Busque materiais por categoria e localização.</p>
          </li>
          <li>
            <b>02</b>
            <strong>Reserve</strong>
            <p>Solicite a reserva diretamente pelo anúncio.</p>
          </li>
          <li>
            <b>03</b>
            <strong>Construa</strong>
            <p>Combine a retirada e dê um novo destino ao material.</p>
          </li>
        </ol>
      </section>
    {modal && (
      <div className="modal-backdrop">
        <section className="modal">
          {error && <p className="modal-error" role="alert">{error}</p>}
            <button className="modal-close" onClick={close}>
              ×
            </button>
            {modal === "ad" ? (
              <>
                <p className="eyebrow">SEU MATERIAL</p>
                <h2>{editing ? "Editar anúncio" : "Publicar material"}</h2>
                <form className="form-grid" onSubmit={submitAd}>
                  <label>
                    Categoria
                    <select
                      value={ad.categoryId}
                      onChange={(e) =>
                        setAd({ ...ad, categoryId: e.target.value })
                      }
                    >
                      {categories.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Título
                    <input
                      required
                      value={ad.title}
                      onChange={(e) => setAd({ ...ad, title: e.target.value })}
                    />
                  </label>
                  <label>
                    Descrição
                    <textarea
                      required
                      value={ad.description}
                      onChange={(e) =>
                        setAd({ ...ad, description: e.target.value })
                      }
                    />
                  </label>
                  <div className="image-field">
                    <span>Imagem principal <small>(JPG, PNG ou WEBP; até 5 MB)</small></span>
                    <label className="file-picker">
                      <input type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => setImage(e.target.files?.[0] ?? null)} />
                      <span className="file-picker-button">Escolher imagem</span>
                      <span className="file-picker-name">{image?.name ?? "Nenhuma imagem selecionada"}</span>
                    </label>
                    {editing?.imageUrl && !image && <small>Manterá a imagem atual se você não escolher uma nova.</small>}
                  </div>
                  <div className="two-columns">
                    <label>
                      Preço (R$)
                      <input
                        required
                        type="number"
                        min="0.01"
                        step="0.01"
                        value={ad.price}
                        onChange={(e) =>
                          setAd({ ...ad, price: e.target.value })
                        }
                      />
                    </label>
                    <label>
                      Quantidade
                      <input
                        required
                        type="number"
                        min="0.01"
                        step="0.01"
                        value={ad.quantity}
                        onChange={(e) =>
                          setAd({ ...ad, quantity: e.target.value })
                        }
                      />
                    </label>
                  </div>
                  <div className="two-columns">
                    <label>
                      Unidade
                      <input
                        required
                        value={ad.unit}
                        onChange={(e) => setAd({ ...ad, unit: e.target.value })}
                      />
                    </label>
                    <label>
                      Condição
                      <select
                        value={ad.condition}
                        onChange={(e) =>
                          setAd({ ...ad, condition: e.target.value })
                        }
                      >
                        <option value="0">Ótimo estado</option>
                        <option value="1">Bom estado</option>
                        <option value="2">Estado regular</option>
                      </select>
                    </label>
                  </div>
                  <div className="two-columns">
                    <label>
                      Cidade
                      <input
                        required
                        value={ad.city}
                        onChange={(e) => setAd({ ...ad, city: e.target.value })}
                      />
                    </label>
                    <label>
                      UF
                      <input
                        required
                        maxLength={2}
                        value={ad.state}
                        onChange={(e) =>
                          setAd({ ...ad, state: e.target.value.toUpperCase() })
                        }
                      />
                    </label>
                  </div>
                  <button className="primary-button" disabled={saving}>
                    {saving ? "Salvando..." : "Salvar anúncio"}
                  </button>
                </form>
              </>
            ) : (
              <>
                <p className="eyebrow">SUA CONTA</p>
                <h2>
                  {modal === "login" ? "Entre na Bricker" : "Crie sua conta"}
                </h2>
                <form className="form-grid" onSubmit={submitAuth}>
                  {modal === "register" && (
                    <label>
                      Nome para exibição
                      <input
                        required
                        value={auth.displayName}
                        onChange={(e) =>
                          setAuth({ ...auth, displayName: e.target.value })
                        }
                      />
                    </label>
                  )}
                  <label>
                    E-mail
                    <input
                      required
                      type="email"
                      value={auth.email}
                      onChange={(e) =>
                        setAuth({ ...auth, email: e.target.value })
                      }
                    />
                  </label>
                  <label>
                    Senha
                    <input
                      required
                      minLength={8}
                      type="password"
                      value={auth.password}
                      onChange={(e) =>
                        setAuth({ ...auth, password: e.target.value })
                      }
                    />
                  </label>
                  {modal === "register" && (
                    <div className="two-columns">
                      <label>
                        Cidade
                        <input
                          value={auth.city}
                          onChange={(e) =>
                            setAuth({ ...auth, city: e.target.value })
                          }
                        />
                      </label>
                      <label>
                        UF
                        <input
                          maxLength={2}
                          value={auth.state}
                          onChange={(e) =>
                            setAuth({
                              ...auth,
                              state: e.target.value.toUpperCase(),
                            })
                          }
                        />
                      </label>
                    </div>
                  )}
                  <button className="primary-button" disabled={saving}>
                    {saving
                      ? "Aguarde..."
                      : modal === "login"
                        ? "Entrar"
                        : "Criar conta"}
                  </button>
                </form>
                <p className="modal-switch">
                  {modal === "login"
                    ? "Ainda não tem conta?"
                    : "Já possui uma conta?"}{" "}
                  <button
                    onClick={() =>
                      setModal(modal === "login" ? "register" : "login")
                    }
                  >
                    {modal === "login" ? "Cadastre-se" : "Entrar"}
                  </button>
                </p>
              </>
            )}
          </section>
        </div>
      )}
    </main>
  );
}
export default App;
