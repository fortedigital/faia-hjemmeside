import styles from './CaseCard.module.scss'

function CaseCard({ track, trackName, title, description, outcomes }) {
  return (
    <article className={styles.card}>
      <div className={styles.trackBadge}>
        <span className={styles.trackLetter}>{track}</span>
        {trackName}
      </div>
      <h3 className={styles.title}>{title}</h3>
      <p className={styles.description}>{description}</p>
      <ul className={styles.outcomes}>
        {outcomes.map((outcome, index) => (
          <li key={index} className={styles.outcome}>
            {outcome}
          </li>
        ))}
      </ul>
    </article>
  )
}

export default CaseCard
