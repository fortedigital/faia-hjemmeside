import { NavLink } from 'react-router-dom'
import Logo from '../Logo/Logo'
import styles from './Header.module.scss'

function Header() {
  return (
    <header className={styles.header}>
      <div className={styles.inner}>
        <NavLink to="/" className={styles.logoLink}>
          <Logo />
        </NavLink>
        <nav className={styles.nav}>
          <NavLink
            to="/"
            end
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.active : ''}`
            }
          >
            Hjem
          </NavLink>
          <NavLink
            to="/caser"
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.active : ''}`
            }
          >
            Caser
          </NavLink>
          <NavLink
            to="/hva-vi-gjor"
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.active : ''}`
            }
          >
            Hva vi gjør
          </NavLink>
        </nav>
      </div>
    </header>
  )
}

export default Header
