import { tracks, outcomes, valueProposition } from '../../data/tracks'
import styles from './HvaViGjor.module.scss'

function HvaViGjor() {
  return (
    <div className={styles.page}>
      {/* Section 1 - Intro */}
      <section className={styles.intro}>
        <h1 className={styles.headline}>AI Accelerator</h1>
        <p className={styles.introText}>
          En strukturert 6-ukers leveransemodell for organisasjoner som har en
          konkret AI use case og ønsker målbare resultater raskt. Vårt
          tverrfaglige team jobber i deres miljø med deres data for å bygge en
          fokusert MVP.
        </p>
      </section>

      {/* Section 2 - De fire sporene */}
      <section className={styles.tracks}>
        {tracks.map((track) => (
          <div key={track.letter} className={styles.track}>
            <div className={styles.trackHeader}>
              <span className={styles.trackLetter}>{track.letter}</span>
              <h2 className={styles.trackName}>{track.name}</h2>
            </div>
            <p className={styles.tagline}>{track.tagline}</p>
            <p className={styles.trackDescription}>{track.description}</p>
            <ul className={styles.examples}>
              {track.examples.map((example) => (
                <li key={example} className={styles.example}>
                  {example}
                </li>
              ))}
            </ul>
          </div>
        ))}
      </section>

      {/* Section 3 - Etter 6 uker */}
      <section className={styles.outcomesSection}>
        <h2 className={styles.sectionHeading}>Etter 6 uker</h2>
        <div className={styles.outcomesGrid}>
          {outcomes.map((outcome) => (
            <div key={outcome.title} className={styles.outcome}>
              <h3 className={styles.outcomeTitle}>{outcome.title}</h3>
              <p className={styles.outcomeDescription}>
                {outcome.description}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* Section 4 - Value proposition */}
      <section className={styles.valueSection}>
        <p className={styles.valueQuote}>{valueProposition}</p>
      </section>
    </div>
  )
}

export default HvaViGjor
