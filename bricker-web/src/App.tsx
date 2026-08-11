import './App.css'

const categories = ['Todos', 'Revestimentos', 'Madeira', 'Hidráulica', 'Elétrica', 'Ferragens']

const listings = [
  { title: 'Porcelanato cinza 60 × 60', price: 'R$ 42,00', unit: 'por m²', quantity: '18 m² disponíveis', city: 'Itajaí, SC', tone: 'sand' },
  { title: 'Portas de madeira maciça', price: 'R$ 380,00', unit: 'cada', quantity: '3 unidades disponíveis', city: 'Balneário Camboriú, SC', tone: 'wood' },
  { title: 'Tijolo ecológico', price: 'R$ 1,25', unit: 'cada', quantity: '800 unidades disponíveis', city: 'Navegantes, SC', tone: 'clay' },
]

function App() {
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
          <div className="search-box" role="search">
            <label><span className="sr-only">Buscar material</span><input placeholder="O que você está procurando?" /></label>
            <label className="location-input"><span className="pin" aria-hidden="true">⌖</span><input placeholder="Cidade ou estado" /></label>
            <button className="primary-button search-button" type="button">Buscar</button>
          </div>
        </div>
        <aside className="impact-card"><span className="impact-icon" aria-hidden="true">↻</span><strong>Menos descarte.<br />Mais construção.</strong><p>Materiais em bom estado merecem uma nova história.</p></aside>
      </section>
      <section className="catalog" id="anuncios">
        <div className="section-heading"><div><p className="eyebrow">MATERIAIS DISPONÍVEIS</p><h2>Encontre o que a sua obra precisa</h2></div><button className="text-button view-all" type="button">Ver todos →</button></div>
        <div className="category-row" aria-label="Categorias de materiais">{categories.map((category, index) => <button className={index === 0 ? 'category active' : 'category'} key={category} type="button">{category}</button>)}</div>
        <div className="listing-grid">{listings.map((listing) => <article className="listing-card" key={listing.title}><div className={`listing-image ${listing.tone}`}><span>Material excedente</span><button aria-label={`Salvar ${listing.title}`} type="button">♡</button></div><div className="listing-content"><p className="condition">EM ÓTIMO ESTADO</p><h3>{listing.title}</h3><p className="price">{listing.price} <small>{listing.unit}</small></p><div className="listing-meta"><span>{listing.quantity}</span><span>{listing.city}</span></div></div></article>)}</div>
      </section>
      <section className="steps" id="como-funciona"><div><p className="eyebrow">SIMPLES E DIRETO</p><h2>Reaproveitar começa aqui.</h2></div><ol><li><span>01</span><strong>Encontre</strong><p>Busque materiais por categoria, preço e localização.</p></li><li><span>02</span><strong>Reserve</strong><p>Solicite a reserva diretamente pelo anúncio.</p></li><li><span>03</span><strong>Construa</strong><p>Combine a retirada e dê um novo destino ao material.</p></li></ol></section>
    </main>
  )
}

export default App
