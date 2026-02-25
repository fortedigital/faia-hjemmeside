import { useState, useRef, useEffect } from 'react'
import { welcomeMessage, getResponse } from '../../data/chatResponses'
import styles from './Chat.module.scss'

function Chat() {
  const [messages, setMessages] = useState([
    { id: 1, text: welcomeMessage, sender: 'bot' }
  ])
  const [inputValue, setInputValue] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const messagesEndRef = useRef(null)
  const nextIdRef = useRef(2)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isTyping])

  function handleSend() {
    const trimmed = inputValue.trim()
    if (!trimmed) return

    const userMessage = {
      id: nextIdRef.current++,
      text: trimmed,
      sender: 'user'
    }

    setMessages(prev => [...prev, userMessage])
    setInputValue('')
    setIsTyping(true)

    setTimeout(() => {
      const responseText = getResponse(trimmed)
      const botMessage = {
        id: nextIdRef.current++,
        text: responseText,
        sender: 'bot'
      }
      setIsTyping(false)
      setMessages(prev => [...prev, botMessage])
    }, 800)
  }

  function handleKeyDown(e) {
    if (e.key === 'Enter') {
      handleSend()
    }
  }

  return (
    <div className={styles.chat}>
      <div className={styles.header}>FAIA-assistenten</div>
      <div className={styles.messages}>
        {messages.map(msg => (
          <div
            key={msg.id}
            className={`${styles.message} ${msg.sender === 'bot' ? styles.bot : styles.user}`}
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
        <input
          className={styles.input}
          type="text"
          placeholder="Fortell oss om din utfordring..."
          value={inputValue}
          onChange={e => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
        />
        <button
          className={styles.sendButton}
          onClick={handleSend}
          disabled={!inputValue.trim()}
        >
          Send
        </button>
      </div>
    </div>
  )
}

export default Chat
