import Header from '../Header/Header'
import styles from './Layout.module.scss'

function Layout({ children }) {
  return (
    <>
      <Header />
      <main className={styles.main}>
        {children}
      </main>
    </>
  )
}

export default Layout
