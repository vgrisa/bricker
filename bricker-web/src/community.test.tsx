import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import { CompleteProfilePage, InterestsPage } from './community'
import type { Profile } from './api'

const connection = {
  on: vi.fn(),
  onreconnected: vi.fn(),
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn().mockResolvedValue(undefined),
}

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl() { return this }
    withAutomaticReconnect() { return this }
    build() { return connection }
  },
}))

const profile: Profile = {
  id: 'user-1', displayName: 'Vinícius', email: 'vini@test.bricker', city: 'Brusque', state: 'SC',
  requiresProfileCompletion: false, createdAtUtc: '2026-01-01T00:00:00Z',
}

describe('módulo de comunidade', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('completa o perfil e normaliza a UF antes de enviar', async () => {
    const setProfile = vi.fn()
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ...profile, requiresProfileCompletion: false }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    render(<MemoryRouter><CompleteProfilePage profile={{ ...profile, requiresProfileCompletion: true, city: undefined, state: undefined }} setProfile={setProfile} /></MemoryRouter>)

    fireEvent.change(screen.getByLabelText('Cidade'), { target: { value: 'Itajaí' } })
    fireEvent.change(screen.getByLabelText('UF'), { target: { value: 'sc' } })
    fireEvent.click(screen.getByRole('button', { name: 'Começar a usar a Bricker' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalled())
    expect(fetchMock.mock.calls[0][1].body).toContain('"state":"SC"')
    expect(setProfile).toHaveBeenCalled()
  })

  it('separa interesses enviados e recebidos e mostra pendência de avaliação', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([
      { interestId: 'i1', conversationId: 'c1', direction: 'sent', interestCreatedAtUtc: '2026-01-01T00:00:00Z', listingId: 'l1', listingTitle: 'Cabos', listingStatus: 1, otherUserId: 'u2', otherUserDisplayName: 'Ana', unreadCount: 0 },
      { interestId: 'i2', conversationId: 'c2', direction: 'received', interestCreatedAtUtc: '2026-01-02T00:00:00Z', listingId: 'l2', listingTitle: 'Portas', listingStatus: 3, otherUserId: 'u3', otherUserDisplayName: 'Bruno', unreadCount: 2, review: { saleId: 's1', status: 'pending', revieweeId: 'u3', revieweeDisplayName: 'Bruno', revieweeRole: 'Comprador', confirmedAtUtc: '2026-01-03T00:00:00Z' } },
    ]), { status: 200 })))
    render(<MemoryRouter><InterestsPage profile={profile} /></MemoryRouter>)

    expect(await screen.findByText('Cabos')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('tab', { name: /Recebidos 1/ }))
    expect(await screen.findByText('Portas')).toBeInTheDocument()
    expect(screen.getByText('Avaliação pendente')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()
  })
})
