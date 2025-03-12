export default function Users(prop : { users: string[] }) {
    return <>
        {prop.users.map(name => (
            <p>{name}</p>
        ))}
    </>
}