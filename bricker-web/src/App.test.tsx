import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from './App'

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl() { return this }
    withAutomaticReconnect() { return this }
    build() { return { on: vi.fn(), start: vi.fn().mockResolvedValue(undefined), stop: vi.fn().mockResolvedValue(undefined), invoke: vi.fn().mockResolvedValue(undefined), onreconnected: vi.fn() } }
  },
}))

describe('rotas e catálogo', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('carrega o catálogo usando os filtros persistidos na URL', async () => {
    const fetchMock = vi.fn((input: string) => {
      if (input.includes('/profile')) return Promise.resolve(new Response('{}', { status: 401 }))
      if (input.includes('/categories')) return Promise.resolve(new Response(JSON.stringify([{ id: 'cat-1', name: 'Elétrica', slug: 'eletrica' }]), { status: 200 }))
      return Promise.resolve(new Response(JSON.stringify({
        items: [{ id: 'listing-1', title: 'Cabos elétricos', description: 'Rolos em bom estado', price: 120, unit: 'rolo', quantity: 2, condition: 0, status: 1, city: 'Brusque', state: 'SC', category: 'Elétrica', categorySlug: 'eletrica', sellerDisplayName: 'Ana', imageUrls: [], createdAtUtc: '2026-01-01T00:00:00Z', hasConfirmedSale: false }],
        totalCount: 1,
      }), { status: 200 }))
    })
    vi.stubGlobal('fetch', fetchMock)
    render(<MemoryRouter initialEntries={['/materiais?search=cabos&category=eletrica&minPrice=100&maxPrice=200']}><App /></MemoryRouter>)

    expect(await screen.findByText('Cabos elétricos')).toBeInTheDocument()
    await waitFor(() => expect(fetchMock.mock.calls.some(([url]) => String(url).includes('search=cabos') && String(url).includes('category=eletrica'))).toBe(true))
  })

  it('redireciona links antigos de conversa para a central de interesses', async () => {
    const fetchMock = vi.fn((input: string) => Promise.resolve(new Response(
      input.includes('/profile')
        ? JSON.stringify({ id: 'user-1', displayName: 'Ana', email: 'ana@test.bricker', requiresProfileCompletion: false, createdAtUtc: '2026-01-01T00:00:00Z' })
        : '[]',
      { status: 200 },
    )))
    vi.stubGlobal('fetch', fetchMock)
    render(<MemoryRouter initialEntries={['/conversas/conversa-1']}><App /></MemoryRouter>)

    expect(await screen.findByRole('heading', { name: 'Interesses' })).toBeInTheDocument()
  })
})
