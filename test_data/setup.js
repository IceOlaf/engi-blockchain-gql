const { CreateDatabaseOperation, DocumentStore } = require('ravendb');

const test_data = require('./test_data.json');

async function setup_raven() {
    const ds = new DocumentStore("http://ravendb:8080", "engi");
    ds.initialize();

    try {
        const options = { databaseName: "engi" };
        const create = new CreateDatabaseOperation(options);
        await ds.maintenance.server.send(create);
    } catch {
    }

    const session = ds.openSession();

    for (const doc of test_data) {
        console.log(`Loading doc ${doc.id}`);
        await session.store(doc.doc, doc.id);
    }
    await session.saveChanges();
    console.log("Data initted");
}

(async () => {
    await setup_raven();
    process.exit();
})()
