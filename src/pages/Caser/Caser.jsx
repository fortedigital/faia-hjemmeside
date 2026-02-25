import { cases } from '../../data/cases'
import CaseCard from '../../components/CaseCard/CaseCard'
import styles from './Caser.module.scss'

function Caser() {
  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <h1 className={styles.headline}>Kundecaser</h1>
        <p className={styles.subtitle}>
          Hver kunde er forskjellig, men problemene følger gjenkjennbare mønstre. Her er eksempler på typiske AI Accelerator-oppdrag.
        </p>
      </header>
      <div className={styles.grid}>
        {cases.map((c) => (
          <CaseCard
            key={c.id}
            track={c.track}
            trackName={c.trackName}
            title={c.title}
            description={c.description}
            outcomes={c.outcomes}
          />
        ))}
      </div>
    </div>
  )
}

export default Caser
