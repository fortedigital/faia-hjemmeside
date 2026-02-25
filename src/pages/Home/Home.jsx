import styles from './Home.module.scss'

function Home() {
  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <h1 className={styles.headline}>
          Fra problem til fungerende AI-løsning på 6 uker
        </h1>
        <p className={styles.subtitle}>
          AI Accelerator gjør et reelt forretningsproblem om til en fungerende
          AI-løsning på 6 uker. Vi bygger i deres miljø, med deres data, og
          måler reell effekt.
        </p>
      </section>
    </div>
  )
}

export default Home
