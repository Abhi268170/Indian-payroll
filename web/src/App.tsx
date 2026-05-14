import { RouterProvider } from 'react-router-dom'
import { router } from './router'

export default function App(): React.ReactElement {
  return <RouterProvider router={router} />
}
