import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Layout from './components/Layout/Layout'
import Home from './pages/Home/Home'
import Caser from './pages/Caser/Caser'
import HvaViGjor from './pages/HvaViGjor/HvaViGjor'

function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/caser" element={<Caser />} />
          <Route path="/hva-vi-gjor" element={<HvaViGjor />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  )
}

export default App
