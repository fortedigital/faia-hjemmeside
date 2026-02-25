import { useState, useRef, useEffect } from 'react'
import { welcomeMessage } from '../../data/chatResponses'
import styles from './Chat.module.scss'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'
const MAX_USER_MESSAGES = 20

function Chat() {
  const [messages, setMessages] = useState(() => {
    const saved = sessionStorage.getItem('faia-chat-messages')
    if (saved) {
      try {
        return JSON.parse(saved)
      } catch {
        return [{ id: 1, sender: 'bot', text: welcomeMessage }]
      }
    }
    return [{ id: 1, sender: 'bot', text: welcomeMessage }]
  })
  const [inputValue, setInputValue] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const messagesEndRef = useRef(null)
  const nextIdRef = useRef(
    messages.reduce((max, m) => Math.max(max, m.id), 0) + 1
  )

  const userMessageCount = messages.filter((m) => m.sender === 'user').length
  const limitReached = userMessageCount >= MAX_USER_MESSAGES

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isTyping])

  useEffect(() => {
    sessionStorage.setItem('faia-chat-messages', JSON.stringify(messages))
  }, [messages])

  const handleSend = async () => {
    const trimmed = inputValue.trim()
    if (!trimmed || isTyping) return

    const userMsg = { id: nextIdRef.current++, sender: 'user', text: trimmed }
    const updatedMessages = [...messages, userMsg]
    setMessages(updatedMessages)
    setInputValue('')
    setIsTyping(true)

    // Build API message history (exclude welcome message, map to role/content)
    const apiMessages = updatedMessages
      .filter((m) => m.id !== 1)
      .map((m) => ({
        role: m.sender === 'user' ? 'user' : 'assistant',
        content: m.text,
      }))

    const botId = nextIdRef.current++

    try {
      const response = await fetch(`${API_URL}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: apiMessages }),
      })

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let botText = ''

      setIsTyping(false)
      setMessages((prev) => [...prev, { id: botId, sender: 'bot', text: '' }])

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        const chunk = decoder.decode(value, { stream: true })
        const lines = chunk.split('\n')

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const data = line.slice(6)
            if (data === '[DONE]') break
            botText += data
            setMessages((prev) =>
              prev.map((m) => (m.id === botId ? { ...m, text: botText } : m))
            )
          }
        }
      }
    } catch (error) {
      setIsTyping(false)
      setMessages((prev) => [
        ...prev,
        {
          id: botId,
          sender: 'bot',
          text: 'Beklager, noe gikk galt. Prøv igjen.',
          isError: true,
        },
      ])
    }
  }

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') handleSend()
  }

  return (
    <div className={styles.chat}>
      <div className={styles.header}>FAIA-assistenten</div>
      <div className={styles.messages}>
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`${styles.message} ${styles[msg.sender]}`}
          >
            {msg.text}
          </div>
        ))}
        {isTyping && (
          <div className={styles.typing}>
            <span className={styles.dot} />
            <span className={styles.dot} />
            <span className={styles.dot} />
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>
      <div className={styles.inputArea}>
        {limitReached ? (
          <div className={styles.limitMessage}>
            Du har nådd maks antall meldinger. Ta kontakt med oss for å gå videre:{' '}
            <a href="mailto:kontakt@faia.no">kontakt@faia.no</a>
          </div>
        ) : (
          <>
            <input
              type="text"
              className={styles.input}
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Fortell oss om din utfordring..."
              maxLength={500}
            />
            <button
              className={styles.sendButton}
              onClick={handleSend}
              disabled={!inputValue.trim() || isTyping}
            >
              Send
            </button>
          </>
        )}
      </div>
    </div>
  )
}

export default Chat
