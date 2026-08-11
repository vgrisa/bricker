import { useCallback, useEffect, useState, type FormEvent } from 'react'
import './App.css'

type Category = { id: string; name: string; slug: string }
type Listing = { id: string; title: string; price: number; unit: string; quantity: number; city: string; state: string; category: string; categorySlug: string; condition: number }
type ListingResult = { items: Listing[]; page: number; pageSize: number; totalCount: number }

const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5190/api/v1'
const tones = ['sand', 'wood', 'clay']

function App() {
  const [categories, setCategories] = useState<Category[]>([])
  const [listings, setListings] = useState<Listing[]>([])
  const [search, setSearch] = useState('')
  const [city, setCity] = useState('')
  const [activeCategory, setActiveCategory] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const loadListings = useCallback(async (filters: { search?: string; city?: string; category?: string } = {}) => {
    setLoading(true)
    setError('')
    try {
      const parameters = new URLSearchParams({ pageSize: '12' })
      if (filters.search?.trim()) parameters.set('search', filters.search.trim())
      if (filters.city?.trim()) parameters.set('city', filters.city.trim())
      if (filters.category) parameters.set('category', filters.category)
      const response = await fetch(`${apiUrl}/listings?${parameters}`)
      if (!response.ok) throw new Error('Não foi possível carregar os anúncios.')
      const result: ListingResult = await response.json()
      setListings(result.items)
    } catch {
      setError('Não foi possível conectar à API. Inicie o backend para ver os materiais disponíveis.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const loadCatalog = async () => {
      try {
        const response = await fetch(`${apiUrl}/categories`)
        if (!response.ok) throw new Error()
        setCategories(await response.json())
      } catch {
        setError('Não foi possível conectar à API. Inicie o backend para ver os materiais disponíveis.')
      }
      await loadListings()
    }
    void loadCatalog()
  }, [loadListings])

  const submitSearch = (event: FormEvent) => {
    event.preventDefault()
    void loadListings({ search, city, category: activeCategory })
  }

  const selectCategory = (slug: string) => {
    setActiveCategory(slug)
    void loadListings({ search, city, category: slug })
  }

  return (
    <main>
      <header className="topbar">
        <a className="brand" href="#inicio">bricker<span>.</span></a>
        <nav aria-label="Navegação principal"><a href="#anuncios">Explorar materiais</a><a href="#como-funciona">Como funciona</a></nav>
        <div className="header-actions"><button className="text-button" type="button">Entrar</button><button className="primary-button" type="button">Anunciar material</button></div>
      </header>
      <section className="hero" id="inicio">
        <div className="hero-copy">
          <p className="eyebrow">CONSTRUÇÃO CIRCULAR</p>
          <h1>O que sobra em uma obra pode construir outra.</h1>
          <p className="hero-description">Encontre materiais de construção excedentes perto de você. Economize na obra e reduza o desperdício.</p>
          <form className="search-box" role="search" onSubmit={submitSearch}>
            <label><span className="sr-only">Buscar material</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="O que você está procurando?" /></label>
            <label className="location-input"><span className="pin" aria-hidden="true">⌖</span><input value={city} onChange={(event) => setCity(event.target.value)} placeholder="Cidade" /></label>
            <button className="primary-button search-button" type="submit">Buscar</button>
          </form>
        </div>
        <aside className="impact-card"><span className="impact-icon" aria-hidden="true">↻</span><strong>Menos descarte.<br />Mais construção.</strong><p>Materiais em bom estado merecem uma nova história.</p></aside>
      </section>
      <section className="catalog" id="anuncios">
        <div className="section-heading"><div><p className="eyebrow">MATERIAIS DISPONÍVEIS</p><h2>Encontre o que a sua obra precisa</h2></div><span className="catalog-count">{listings.length} materiais encontrados</span></div>
        <div className="category-row" aria-label="Categorias de materiais">
          <button className={activeCategory === '' ? 'category active' : 'category'} onClick={() => selectCategory('')} type="button">Todos</button>
          {categories.map((category) => <button className={activeCategory === category.slug ? 'category active' : 'category'} onClick={() => selectCategory(category.slug)} key={category.id} type="button">{category.name}</button>)}
        </div>
        {error && <p className="catalog-message error-message">{error}</p>}
        {loading && <p className="catalog-message">Carregando materiais...</p>}
        {!loading && !error && listings.length === 0 && <p className="catalog-message">Nenhum material encontrado com esses filtros.</p>}
        <div className="listing-grid">{listings.map((listing, index) => <article className="listing-card" key={listing.id}><div className={`listing-image ${tones[index % tones.length]}`}><span>{listing.category}</span><button aria-label={`Salvar ${listing.title}`} type="button">♡</button></div><div className="listing-content"><p className="condition">{listing.condition === 0 ? 'EM ÓTIMO ESTADO' : 'EM BOM ESTADO'}</p><h3>{listing.title}</h3><p className="price">{listing.price.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} <small>por {listing.unit}</small></p><div className="listing-meta"><span>{listing.quantity} {listing.unit} disponíveis</span><span>{listing.city}, {listing.state}</span></div></div></article>)}</div>
      </section>
      <section className="steps" id="como-funciona"><div><p className="eyebrow">SIMPLES E DIRETO</p><h2>Reaproveitar começa aqui.</h2></div><ol><li><span>01</span><strong>Encontre</strong><p>Busque materiais por categoria, preço e localização.</p></li><li><span>02</span><strong>Reserve</strong><p>Solicite a reserva diretamente pelo anúncio.</p></li><li><span>03</span><strong>Construa</strong><p>Combine a retirada e dê um novo destino ao material.</p></li></ol></section>
    </main>
  )
}

export default App
