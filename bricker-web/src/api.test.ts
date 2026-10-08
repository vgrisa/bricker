import { api, fileUrl } from './api'

describe('cliente da API', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('inclui cookies e converte respostas JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(api<{ ok: boolean }>('/profile')).resolves.toEqual({ ok: true })
    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/api/v1/profile'), expect.objectContaining({ credentials: 'include' }))
  })

  it('expõe a mensagem de validação retornada pela API', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ message: 'Campo inválido.' }), { status: 400 })))

    await expect(api('/profile')).rejects.toThrow('Campo inválido.')
  })

  it('mantém URLs externas de imagem e converte uploads relativos', () => {
    expect(fileUrl('https://example.com/image.webp')).toBe('https://example.com/image.webp')
    expect(fileUrl('/uploads/test.webp')).toContain('/uploads/test.webp')
  })
})
