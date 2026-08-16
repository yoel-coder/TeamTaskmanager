import { useEffect, useMemo, useState } from 'react'

const api = async (path, options) => {
  const token = localStorage.getItem('task-token')
  const response = await fetch(`/api${path}`, { headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, ...options })
  if (!response.ok) throw new Error((await response.json().catch(() => ({}))).message || 'Something went wrong.')
  return response.json()
}

const formatDate = value => value ? new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' }) : '—'

function App() {
  const [userName, setUserName] = useState(localStorage.getItem('task-user') || '')
  const [loginName, setLoginName] = useState('')
  const [password, setPassword] = useState('')
  const [registering, setRegistering] = useState(false)
  const [tasks, setTasks] = useState([])
  const [form, setForm] = useState({ title: '', description: '', dueDate: '' })
  const [error, setError] = useState('')

  const loadTasks = async () => { try { setTasks(await api('/tasks')); setError('') } catch (e) { setError(e.message) } }
  useEffect(() => { if (userName) loadTasks() }, [userName])
  const active = useMemo(() => tasks.filter(task => task.status !== 'completed'), [tasks])
  const completed = useMemo(() => tasks.filter(task => task.status === 'completed'), [tasks])

  const login = async event => {
    event.preventDefault()
    try {
      const result = await api(`/auth/${registering ? 'register' : 'login'}`, { method: 'POST', body: JSON.stringify({ userName: loginName, password }) })
      localStorage.setItem('task-token', result.token); localStorage.setItem('task-user', result.userName)
      setUserName(result.userName); setError('')
    } catch (e) { setError(e.message || 'Login failed.') }
  }
  const logout = async () => { try { await api('/auth/logout', { method: 'POST' }) } catch {} localStorage.removeItem('task-token'); localStorage.removeItem('task-user'); setUserName(''); setLoginName(''); setPassword('') }
  const addTask = async event => {
    event.preventDefault()
    try {
      await api('/tasks', { method: 'POST', body: JSON.stringify({ ...form, dueDate: form.dueDate || null }) })
      setForm({ title: '', description: '', dueDate: '' }); await loadTasks()
    } catch (e) { setError(e.message) }
  }
  const act = async (task, action) => { try { await api(`/tasks/${task.id}/${action}`, { method: 'POST' }); await loadTasks() } catch (e) { setError(e.message) } }

  if (!userName) return <main className="login-page"><form className="login-card" onSubmit={login}><div className="logo">✓</div><h1>Team Task Manager</h1><p>{registering ? 'Create an account for your team workspace.' : 'Sign in to your team workspace.'}</p>{error && <div className="error">{error}</div>}<input autoFocus required minLength="3" value={loginName} onChange={e => setLoginName(e.target.value)} placeholder="Username" /><input required minLength="8" type="password" value={password} onChange={e => setPassword(e.target.value)} placeholder="Password" /><button>{registering ? 'Create account' : 'Sign in'}</button><button type="button" className="switch-auth" onClick={() => { setRegistering(!registering); setError('') }}>{registering ? 'Already registered? Sign in' : 'New user? Create account'}</button></form></main>

  return <main className="app-shell">
    <header><div><span className="eyebrow">TEAM WORKSPACE</span><h1>Good work starts here.</h1><p>Plan, claim, and complete tasks together.</p></div><div className="user"><span>{userName.charAt(0).toUpperCase()}</span><div><b>{userName}</b><button onClick={logout}>Sign out</button></div></div></header>
    {error && <div className="error">{error}<button onClick={() => setError('')}>×</button></div>}
    <section className="stats"><article><b>{active.length}</b><span>Active tasks</span></article><article><b>{active.filter(t => t.status === 'in-progress').length}</b><span>In progress</span></article><article><b>{completed.length}</b><span>Completed</span></article></section>
    <section className="workspace">
      <form className="new-task" onSubmit={addTask}><h2>Add a task</h2><label>Task title<input required value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} placeholder="What needs to be done?" /></label><label>Description<textarea value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} placeholder="Add helpful details" /></label><label>Due date<input type="date" value={form.dueDate} onChange={e => setForm({ ...form, dueDate: e.target.value })} /></label><button>Add task</button></form>
      <div className="boards"><TaskList title="Current tasks" tasks={active} empty="No active tasks yet." userName={userName} act={act} /><TaskList title="Completed" tasks={completed} empty="Completed tasks will appear here." userName={userName} act={act} completed /></div>
    </section>
  </main>
}

function TaskList({ title, tasks, empty, userName, act, completed }) {
  return <section className="task-list"><div className="list-heading"><h2>{title}</h2><span>{tasks.length}</span></div>{!tasks.length && <div className="empty">{empty}</div>}{tasks.map(task => <article className="task" key={task.id}><div className="task-top"><span className={`status ${task.status}`}>{task.status.replace('-', ' ')}</span><small>Due {formatDate(task.dueDate)}</small></div><h3>{task.title}</h3>{task.description && <p>{task.description}</p>}<div className="meta">Created by <b>{task.createdBy}</b>{task.assignedTo && <> · Working: <b>{task.assignedTo}</b></>}{task.completedBy && <> · Done by <b>{task.completedBy}</b> on {formatDate(task.completedAt)}</>}</div>{!completed && task.status === 'open' && <button className="action" onClick={() => act(task, 'start')}>Start working</button>}{!completed && task.status === 'in-progress' && <button className="action complete" disabled={task.assignedTo.toLowerCase() !== userName.toLowerCase()} onClick={() => act(task, 'complete')}>{task.assignedTo.toLowerCase() === userName.toLowerCase() ? 'Mark completed' : `Being worked on by ${task.assignedTo}`}</button>}</article>)}</section>
}

export default App
