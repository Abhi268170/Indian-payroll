import { MAILHOG_URL } from './env'

/** MailHog is the email client (the user's inbox), not the application backend.
 *  Reading the set-password link here is equivalent to a real user opening their
 *  email — the OrgAdmin password flow is email-gated by design and has no other
 *  UI path. This is the only non-app network call the tests make. */

interface MailHogMessage {
  Content: { Body: string; Headers: Record<string, string[]> }
  To: { Mailbox: string; Domain: string }[]
}

async function fetchMessages(): Promise<MailHogMessage[]> {
  const res = await fetch(`${MAILHOG_URL}/api/v2/messages?limit=100`)
  if (!res.ok) throw new Error(`MailHog fetch failed: ${res.status}`)
  const data = (await res.json()) as { items: MailHogMessage[] }
  return data.items ?? []
}

function decodeQuotedPrintable(body: string): string {
  return body.replace(/=\r\n/g, '').replace(/=3D/g, '=')
}

/** Poll MailHog for the most recent set-password link addressed to `recipient`. */
export async function getSetPasswordLink(recipient: string, timeoutMs = 15_000): Promise<string> {
  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    const messages = await fetchMessages()
    for (const m of messages) {
      const to = m.To.map((t) => `${t.Mailbox}@${t.Domain}`)
      if (!to.includes(recipient)) continue
      const body = decodeQuotedPrintable(m.Content.Body)
      const match = body.match(/https?:\/\/[^\s"<>]+set-password[^\s"<>]+/)
      if (match) {
        // Strip a trailing path segment that HTML entities may have appended.
        return match[0].replace(/&amp;/g, '&')
      }
    }
    await new Promise((r) => setTimeout(r, 1000))
  }
  throw new Error(`No set-password email for ${recipient} within ${timeoutMs}ms`)
}
